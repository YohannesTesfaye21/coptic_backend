using coptic_app_backend.Domain.Models;

namespace coptic_app_backend.Domain.Interfaces
{
    /// <summary>
    /// Interface for video compression service
    /// </summary>
    public interface IVideoCompressionService
    {
        /// <summary>
        /// Compress a video stream for mobile streaming
        /// </summary>
        /// <param name="inputStream">Input video stream</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="quality">Compression quality level</param>
        /// <returns>Compressed video stream</returns>
        Task<Stream> CompressVideoAsync(Stream inputStream, string fileName, VideoQuality quality = VideoQuality.Mobile);

        /// <summary>
        /// Compress a video from URL for mobile streaming
        /// </summary>
        /// <param name="videoUrl">URL of the video to compress</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="quality">Compression quality level</param>
        /// <returns>Compressed video stream</returns>
        Task<Stream> CompressVideoFromUrlAsync(string videoUrl, string fileName, VideoQuality quality = VideoQuality.Mobile);

        /// <summary>
        /// Check if video compression is needed based on file size and quality requirements
        /// </summary>
        /// <param name="inputStream">Input video stream</param>
        /// <param name="fileSize">File size in bytes</param>
        /// <param name="quality">Target quality level</param>
        /// <returns>True if compression is needed</returns>
        Task<bool> IsVideoCompressionNeededAsync(Stream inputStream, long fileSize, VideoQuality quality = VideoQuality.Mobile);
    }
}
