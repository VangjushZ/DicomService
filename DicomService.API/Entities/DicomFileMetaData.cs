namespace DicomService.API.Entities
{
    public class DicomFileMetaData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        public string? PreviewPath { get; set; }

    }
}
