using FellowOakDicom;

namespace DicomService.API.Interfaces
{
    /// <summary>
    /// Abstraction for DICOM file parsing
    /// </summary>
    public interface IDicomParser
    {
        /// <summary>
        /// Parses a DICOM file and returns the value of the specified tag
        /// </summary>
        /// <param name="dicomStream">Stream of raw DICOM data</param>
        /// <param name="tag">The DICOM tag identifer, e.g. (0002,0000)</param>
        Task<string> GetTagValueAsync(Stream dicomStream, string tag);

        /// <summary>
        /// Validates if the provided stream is a valid DICOM file
        /// </summary>
        /// <param name="dicomStream">Stream of raw data to validate it is DICOM</param>
        Task<bool> ValidateAsync(Stream dicomStream);

        /// <summary>
        /// Loads a DICOM dataset from the provided stream
        /// </summary>
        /// <param name="dicomStream">Stream of raw data</param>
        Task<DicomDataset> LoadDicomDatasetAsync(Stream dicomStream);

        /// <summary>
        /// Get number for frames in a DicomDataset
        /// </summary>
        int GetNumberOfFrames(DicomDataset dataset);

        /// <summary>
        /// Renders a specific frame of the DICOM dataset as a PNG image
        /// </summary>
        /// <param name="ds">DicomDataset to render</param>
        /// <param name="frameIdx">Zero-based index of the frame to render</param>
        Task<byte[]> RenderFrameAsPngAsync(DicomDataset ds, int frameIdx);
    }
}
