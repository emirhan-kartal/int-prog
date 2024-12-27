public class FileEntity
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string OriginalFileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; } // in bytes
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string UserId { get; set; }
    public int? FolderId { get; set; }

    public virtual ApplicationUser User { get; set; }
    public virtual Folder Folder { get; set; }
} 