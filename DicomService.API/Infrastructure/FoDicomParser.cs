using DicomService.API.Entities;
using DicomService.API.Interfaces;
using FellowOakDicom;
using FellowOakDicom.Imaging;
using SixLabors.ImageSharp;
namespace DicomService.API.Infrastructure
{
    /// <summary>
    /// Implementation of IDicomParser using FoDicom library
    /// </summary>
    public class FoDicomParser : IDicomParser
    {
        public async Task<string> GetTagValueAsync(Stream dicomStream, string tag)
        {

            ArgumentNullException.ThrowIfNull(dicomStream);

            if (string.IsNullOrWhiteSpace(tag))
                throw new ArgumentException("Tag cannot be null or empty", nameof(tag));    

            // Reset position of the stream if it supports seeking
            if (dicomStream.CanSeek)
                dicomStream.Position = 0;

            // Use the FoDicom library to read the DICOM file (throws if not valid DICOM)
            var dicomFile = await DicomFile.OpenAsync(dicomStream);
            var ds = dicomFile.Dataset;

            DicomTag dicomTag;
            try
            {
                // Parse the tag string into a DicomTag object
                dicomTag = DicomTag.Parse(tag);
            }
            catch (DicomDataException ex)
            {
                throw new ArgumentException($"Invalid DICOM tag format: {tag} - expected '0002,0000' or '(0002,0000)'", nameof(tag), ex);
            }

            // Attempt to retrieve the value of the tag
            if (ds.TryGetSingleValue(dicomTag, out string value))
                return value;

            throw new KeyNotFoundException($"Tag {tag} not found in DICOM file");
            
        }

        public async Task<bool> ValidateAsync(Stream dicomStream)
        {
            ArgumentNullException.ThrowIfNull(dicomStream);

            // Reset position of the stream if it supports seeking
            if (dicomStream.CanSeek)
                dicomStream.Position = 0;
            try
            {
                // Use the FoDicom library to read the DICOM file (throws if not valid DICOM)
                await DicomFile.OpenAsync(dicomStream);
                return true;
            }
            catch (DicomDataException)
            {
                return false;
            }
            finally
            {   
                // Reset the stream position back to the beginning
                if (dicomStream.CanSeek)
                    dicomStream.Position = 0;
            }
        }

        public async Task<DicomDataset> LoadDicomDatasetAsync(Stream dicomStream)
        {
            if (dicomStream.CanSeek)
                dicomStream.Position = 0;
            var dcm = await DicomFile.OpenAsync(dicomStream);
            if(dicomStream.CanSeek)
                dicomStream.Position = 0;
            return dcm.Dataset;
        }

        public int GetNumberOfFrames(DicomDataset dataset)
        {
            return new DicomImage(dataset).NumberOfFrames;
        }

        public async Task<byte[]> RenderFrameAsPngAsync(DicomDataset ds, int frameIdx)
        {
            var total = GetNumberOfFrames(ds);

            if (frameIdx < 0 || frameIdx >= total)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIdx), $"Frame index {frameIdx} is out of range. Total frames: {total}");
            }

            var image = new DicomImage(ds, frameIdx);
            if (image == null)
                throw new InvalidOperationException("Failed to create DicomImage from dataset");

            using var sharpImg = image.RenderImage().AsSharpImage();

            await using var ms = new MemoryStream();
            await sharpImg.SaveAsPngAsync(ms);

            return ms.ToArray();
        }
    }
}
