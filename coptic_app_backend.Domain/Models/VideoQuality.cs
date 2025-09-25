namespace coptic_app_backend.Domain.Models
{
    /// <summary>
    /// Video quality levels for compression and streaming
    /// </summary>
    public enum VideoQuality
    {
        /// <summary>
        /// Low quality - 480p, 500k bitrate, 50MB max
        /// </summary>
        Low = 0,
        
        /// <summary>
        /// Mobile quality - 720p, 1000k bitrate, 100MB max (default)
        /// </summary>
        Mobile = 1,
        
        /// <summary>
        /// Medium quality - 1080p, 2000k bitrate, 200MB max
        /// </summary>
        Medium = 2,
        
        /// <summary>
        /// High quality - 1080p, 4000k bitrate, 300MB max
        /// </summary>
        High = 3
    }
}

