using DicomService.API.Controllers;
using DicomService.API.Data;
using DicomService.API.Dtos;
using DicomService.API.Entities;
using DicomService.API.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using FellowOakDicom;

namespace DicomService.Tests
{
    public class DicomControllerTests
    {
        private ApplicationDbContext CreateDb(string? name = null)
        {
            var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(opts);
        }

        // GET /api/dicom empty list
        [Fact]
        public async Task GetFiles_EmptyDb_ReturnsEmpty()
        {
            var db = CreateDb();
            var ctl = new DicomController(db, Mock.Of<IFileStore>(), Mock.Of<IDicomParser>());

            var res = await ctl.GetFiles();

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<DicomDto>>(ok.Value);
            Assert.Empty(list);
        }

        // GET /api/dicom non-empty list
        [Fact]
        public async Task GetFiles_WithData_ReturnsDtos()
        {
            var db = CreateDb();
            db.DicomFiles.Add(new DicomFileMetaData { FileName = "a.dcm", UploadedAt = DateTime.UtcNow });
            db.DicomFiles.Add(new DicomFileMetaData { FileName = "b.dcm", UploadedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();

            var ctl = new DicomController(db, Mock.Of<IFileStore>(), Mock.Of<IDicomParser>());

            var res = await ctl.GetFiles();
            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<DicomDto>>(ok.Value).ToList();

            Assert.Equal(2, list.Count);
            Assert.Contains(list, d => d.FileName == "a.dcm");
            Assert.Contains(list, d => d.FileName == "b.dcm");
        }

        // POST upload with null file
        [Fact]
        public async Task Upload_NullFile_ReturnsBadRequest()
        {
            var ctl = new DicomController(CreateDb(), Mock.Of<IFileStore>(), Mock.Of<IDicomParser>());

            var res = await ctl.Upload(file: null);
            var bad = Assert.IsType<BadRequestObjectResult>(res.Result);
            var pd = Assert.IsType<ProblemDetails>(bad.Value);
            Assert.Equal("File is required", pd.Title);
        }

        // POST upload invalid DICOM
        [Fact]
        public async Task Upload_InvalidDicom_ReturnsBadRequest()
        {
            var parser = new Mock<IDicomParser>();
            parser.Setup(p => p.ValidateAsync(It.IsAny<Stream>())).ReturnsAsync(false);

            var ctl = new DicomController(CreateDb(), Mock.Of<IFileStore>(), parser.Object);

            var fakeFile = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "x.dcm");
            var res = await ctl.Upload(fakeFile);

            var bad = Assert.IsType<BadRequestObjectResult>(res.Result);
            var pd = Assert.IsType<ProblemDetails>(bad.Value);
            Assert.Equal("Invalid DICOM file", pd.Title);
        }

        // GET header missing file metadata
        [Fact]
        public async Task GetHeader_NoMeta_ReturnsNotFound()
        {
            var ctl = new DicomController(CreateDb(), Mock.Of<IFileStore>(), Mock.Of<IDicomParser>());

            var res = await ctl.GetHeader(Guid.NewGuid(), tag: "0002,0000");
            var nf = Assert.IsType<NotFoundObjectResult>(res.Result);
            var pd = Assert.IsType<ProblemDetails>(nf.Value);
            Assert.Equal("File not found", pd.Title);
        }

        // GET header invalid tag format
        [Fact]
        public async Task GetHeader_InvalidTag_ThrowsArgument_ReturnsBadRequest()
        {
            var db = CreateDb();
            var meta = new DicomFileMetaData { FileName = "x", FilePath = "p" };
            db.DicomFiles.Add(meta);
            await db.SaveChangesAsync();

            var fs = new Mock<IFileStore>();
            fs.Setup(f => f.GetFileAsync(meta.FilePath))
              .ReturnsAsync(new MemoryStream());

            var parser = new Mock<IDicomParser>();
            parser.Setup(p => p.GetTagValueAsync(It.IsAny<Stream>(), "bad"))
                  .ThrowsAsync(new ArgumentException("bad format"));

            var ctl = new DicomController(db, fs.Object, parser.Object);

            var res = await ctl.GetHeader(meta.Id, tag: "bad");
            var bad = Assert.IsType<BadRequestObjectResult>(res.Result);
            var pd = Assert.IsType<ProblemDetails>(bad.Value);
            Assert.Equal("Invalid tag format", pd.Title);
        }

        // GET image missing file in store
        [Fact]
        public async Task GetImage_FileNotOnDisk_ReturnsNotFound()
        {
            var db = CreateDb();
            var meta = new DicomFileMetaData { FileName = "x", FilePath = "p" };
            db.DicomFiles.Add(meta);
            await db.SaveChangesAsync();

            var fs = new Mock<IFileStore>();
            fs.Setup(f => f.GetFileAsync("p"))
              .ThrowsAsync(new FileNotFoundException("not here"));

            var ctl = new DicomController(db, fs.Object, Mock.Of<IDicomParser>());

            var res = await ctl.GetImage(meta.Id, frame: null);
            var nf = Assert.IsType<NotFoundObjectResult>(res);
            var pd = Assert.IsType<ProblemDetails>(nf.Value);
            Assert.Equal("File not found", pd.Title);
        }

        // GET image invalid frame index
        [Fact]
        public async Task GetImage_InvalidFrame_ReturnsBadRequest()
        {
            var db = CreateDb();
            var meta = new DicomFileMetaData { FileName = "x", FilePath = "p" };
            db.DicomFiles.Add(meta);
            await db.SaveChangesAsync();

            var fs = new Mock<IFileStore>();
            fs.Setup(f => f.GetFileAsync("p"))
              .ReturnsAsync(new MemoryStream());

            var parser = new Mock<IDicomParser>();
            parser.Setup(p => p.LoadDicomDatasetAsync(It.IsAny<Stream>()))
              .ReturnsAsync(new DicomDataset());

            parser.Setup(p => p.GetNumberOfFrames(It.IsAny<DicomDataset>()))
              .Returns(1);

            var ctl = new DicomController(db, fs.Object, parser.Object);

            var res = await ctl.GetImage(meta.Id, frame: 5);
            var bad = Assert.IsType<BadRequestObjectResult>(res);
            var pd = Assert.IsType<ProblemDetails>(bad.Value);
            Assert.Equal("Invalid frame index", pd.Title);
        }

        // GET image success
        [Fact]
        public async Task GetImage_Valid_ReturnsPng()
        {
            var db = CreateDb();
            var meta = new DicomFileMetaData { FileName = "x", FilePath = "p" };
            db.DicomFiles.Add(meta);
            await db.SaveChangesAsync();

            var fs = new Mock<IFileStore>();
            fs.Setup(f => f.GetFileAsync("p"))
              .ReturnsAsync(new MemoryStream());

            var parser = new Mock<IDicomParser>();
            parser.Setup(p => p.LoadDicomDatasetAsync(It.IsAny<Stream>()))
                  .ReturnsAsync(new DicomDataset());
            parser.Setup(p => p.GetNumberOfFrames(It.IsAny<DicomDataset>()))
                  .Returns(1);
            parser
              .Setup(p => p.RenderFrameAsPngAsync(It.IsAny<DicomDataset>(), 0))
              .ReturnsAsync(new byte[] { 1, 2, 3 });

            var ctl = new DicomController(db, fs.Object, parser.Object);

            var res = await ctl.GetImage(meta.Id, frame: 0);
            var fileRes = Assert.IsType<FileContentResult>(res);
            Assert.Equal("image/png", fileRes.ContentType);
            Assert.Equal(new byte[] { 1, 2, 3 }, fileRes.FileContents);
        }
    }
}
