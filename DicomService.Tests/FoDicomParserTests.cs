using DicomService.API.Infrastructure;

namespace DicomService.Tests
{
    public class FoDicomParserTests
    {

        private readonly FoDicomParser _parser = new FoDicomParser();

        private Stream OpenExampleDicom() =>
            File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", "IM000002"));

        [Fact]
        public async Task ValidateAsync_ValidDicom_ReturnsTrue()
        {
            using var fs = OpenExampleDicom();
            Assert.True(await _parser.ValidateAsync(fs));
        }

        [Fact]
        public async Task ValidateAsync_RandomBytes_ReturnsFalse()
        {
            using var fs = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
            Assert.False(await _parser.ValidateAsync(fs));
        }

        [Fact]
        public async Task GetTagValueAsync_KnownTag_ReturnsValue()
        {
            using var fs = OpenExampleDicom();
            var name = await _parser.GetTagValueAsync(fs, "0010,0010");
            Assert.False(string.IsNullOrWhiteSpace(name));
        }

        [Fact]
        public async Task GetTagValueAsync_BadTag_ThrowsArgumentException()
        {
            using var fs = OpenExampleDicom();
            await Assert.ThrowsAsync<ArgumentException>(
                () => _parser.GetTagValueAsync(fs, "BAD,TAG"));
        }

        [Fact]
        public async Task GetTagValueAsync_MissingTag_ThrowsKeyNotFoundException()
        {
            using var fs = OpenExampleDicom();
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _parser.GetTagValueAsync(fs, "9999,9999"));
        }

        [Fact]
        public async Task RenderFrameAsPngAsync_FrameOutOfRange_Throws()
        {
            using var fs = OpenExampleDicom();
            var ds = await _parser.LoadDicomDatasetAsync(fs);
            int frames = _parser.GetNumberOfFrames(ds);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => _parser.RenderFrameAsPngAsync(ds, frames));
        }
    }
}
