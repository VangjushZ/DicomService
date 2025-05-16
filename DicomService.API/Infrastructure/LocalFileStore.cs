using DicomService.API.Interfaces;

namespace DicomService.API.Infrastructure
{
    public class LocalFileStore : IFileStore
    {
        private readonly string _basePath;
        public LocalFileStore(IConfiguration configuration, IHostEnvironment env)
        {
            var folder = configuration["FileStore:Folder"] ?? "dicom-uploads";
            _basePath = Path.Combine(env.ContentRootPath, folder);

            Directory.CreateDirectory(_basePath);
        }
        public async Task<string> SaveFileAsync(Stream stream, string originalFileName)
        {
            // Prevent path traversal attacks by obtaining only the name portion of the file
            // and removing invalid characters
            var safeFileName = Path.GetFileName(originalFileName);
            var invalidChars = Path.GetInvalidFileNameChars();
            var cleanedFileName = new string(safeFileName.Where(c => !invalidChars.Contains(c)).ToArray());

            var ext = Path.GetExtension(cleanedFileName);
            var baseName = Path.GetFileNameWithoutExtension(cleanedFileName);

            // Add a GUID to the file name to ensure uniqueness allowing files of the same name
            // to be uploaded
            var fileName = $"{baseName}_{Guid.NewGuid():N}{ext}";

            var fullPath = Path.Combine(_basePath, fileName);

            // Write the file and dispose of the stream
            await using var fs = File.Create(fullPath);
            await stream.CopyToAsync(fs);

            return fileName;

        }

        public Task<Stream> GetFileAsync(string filePath)
        {
            var fullPath = Path.Combine(_basePath, filePath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found in local directory {_basePath}");
            }

            return Task.FromResult<Stream>(File.OpenRead(fullPath));
        }
    }
}
