namespace DicomService.API.Interfaces
{
    /// <summary>
    /// Abstraction for file storage with saving and retrieval functinality
    /// </summary>
    public interface IFileStore
    {
        /// <summary>
        /// Saves a file to the store and returns the file path
        /// </summary>
        /// <param name="stream">File data to save</param>
        /// <param name="originalFileName">Original file name</param>
        /// <returns>The relative path of the saved file</returns>
        Task<string> SaveFileAsync(Stream stream, string originalFileName);

        /// <summary>
        /// Retrieves a file from the store
        /// </summary>
        /// <param name="filePath">The path of the file to retrieve</param>
        /// <returns>The retrieved file</returns>
        Task<Stream> GetFileAsync(string filePath);

    }
}
