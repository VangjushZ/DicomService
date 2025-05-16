using DicomService.API.Data;
using DicomService.API.Dtos;
using DicomService.API.Entities;
using DicomService.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using Microsoft.EntityFrameworkCore;

namespace DicomService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DicomController : ControllerBase
    {
        private readonly ApplicationDbContext _dbcontext;
        private readonly IFileStore _fileStore;
        private readonly IDicomParser _dicomParser;

        public DicomController(
            ApplicationDbContext dbContext,
            IFileStore fileStore,
            IDicomParser dicomParser)
        {
            _dbcontext = dbContext;
            _fileStore = fileStore;
            _dicomParser = dicomParser;
        }

        /// <summary>
        /// GET /api/dicom
        /// Returns a list of all uploaded DICOM files
        /// </summary>
        /// <returns>Returns JSON array of <see cref="DicomDto"/></returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DicomDto>>> GetFiles()
        {
            var files = await _dbcontext.DicomFiles.OrderByDescending(k => k.UploadedAt).AsNoTracking().ToListAsync();

            var result = files.Select(f => new DicomDto
            {
                Id = f.Id,
                FileName = f.FileName,
                UploadedAt = f.UploadedAt
            });
            return Ok(result);
        }

        /// <summary>
        /// Get the DICOM file header value for a given file ID and tag
        /// </summary>
        /// <param name="id">GUID of the stored DICOM file</param>
        /// <param name="tag">DICOM tag in "GGGG,EEEE" format (e.g. "0002,0000"</param>
        /// <returns>Returns <see cref="HeaderResponse"/> containg the tag and its values</returns>
        [HttpGet("{id:guid}/header")]
        public async Task<ActionResult<HeaderResponse>> GetHeader(Guid id, [FromQuery] string tag)
        {

            if (string.IsNullOrWhiteSpace(tag))
                return BadRequest(new ProblemDetails { Title = "Tag is required" });

            var meta = await _dbcontext.DicomFiles.FindAsync(id);
            if (meta == null)
                return NotFound(new ProblemDetails { Title = "File not found" });

            var stream = await _fileStore.GetFileAsync(meta.FilePath);

            string value;
            try
            {
                value = await _dicomParser.GetTagValueAsync(stream, tag);
            }
            catch (ArgumentException ae)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid tag format", Detail = ae.Message });
            }
            catch (KeyNotFoundException knfe)
            {
                return NotFound(new ProblemDetails { Title = "Tag not found", Detail = knfe.Message });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while reading the DICOM header"
                });
            }

            return Ok(new HeaderResponse
            {
                Tag = tag,
                Value = value
            });
        }

        /// <summary>
        /// GET /api/dicom/{id}/image
        /// Renders the specified frame of the given DICOM file as an inline PNG image
        /// </summary>
        /// <param name="id"> GUID of the stored DICOM file to render</param>
        /// <param name="frame">Zero-based index of the frame to render (default is 0)</param>
        /// <returns>PNG bytes rendered inline for browser viewing</returns>
        [HttpGet("{id:guid}/image")]
        public async Task<ActionResult> GetImage(Guid id, [FromQuery]int? frame)
        {
            var meta = await _dbcontext.DicomFiles.FindAsync(id);
            if (meta == null)
                return NotFound(new ProblemDetails { Title = "File not found" });

            Stream dicomStream;
            try
            {
                dicomStream = await _fileStore.GetFileAsync(meta.FilePath);
            }
            catch (FileNotFoundException fe)
            {
                return NotFound(new ProblemDetails { Title = "File not found", Detail = fe.Message });
            }


            await using (dicomStream)
            {
                if (!await _dicomParser.ValidateAsync(dicomStream))
                    return BadRequest(new ProblemDetails { Title = "Invalid DICOM file" });

                var ds = await _dicomParser.LoadDicomDatasetAsync(dicomStream);

                var totalFrames = _dicomParser.GetNumberOfFrames(ds);
                var idx = frame ?? 0;
                if (idx < 0 || idx >= totalFrames)
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Invalid frame index",
                        Detail = $"Frame index must be between 0 and {totalFrames - 1}"
                    });

                var png = await _dicomParser.RenderFrameAsPngAsync(ds, idx);
                return File(png, "image/png");
            }
        }

        /// <summary>
        /// POST /api/dicom/upload
        /// Uploads a DICOM file, validates it, and saves it to the file store
        /// </summary>
        /// <returns>Returns <see cref="UploadResponse"/> containing the file ID, name, and path</returns>
        [HttpPost("upload")]
        public async Task<ActionResult<UploadResponse>> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new ProblemDetails { Title = "File is required" });

            await using (var validationStream = file.OpenReadStream())
            {
                if (!await _dicomParser.ValidateAsync(validationStream))
                    return BadRequest(new ProblemDetails { Title = "Invalid DICOM file" });
            }

            string savedPath;
            try
            {
                await using (var stream = file.OpenReadStream())
                {
                    savedPath = await _fileStore.SaveFileAsync(stream, file.FileName);
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = "An error occurred while saving the file"
                });
            }

            var dicomFile = new DicomFileMetaData
            {
                FilePath = savedPath,
                FileName = file.FileName,
            };

            await _dbcontext.DicomFiles.AddAsync(dicomFile);
            await _dbcontext.SaveChangesAsync();

            return Ok(new UploadResponse
            {
                Id = dicomFile.Id,
                FileName = dicomFile.FileName,
                FilePath = dicomFile.FilePath
            });
        }
    }
}
