namespace coptic_app_backend.Domain.Models
{
    /// <summary>
    /// Media file information
    /// </summary>
    public class MediaFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string ObjectName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime LastModified { get; set; }
        public MediaType MediaType { get; set; }
        public string FolderId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Media type enumeration
    /// </summary>
    public enum MediaType
    {
        Book,
        Video,
        Audio,
        Other
    }
}

