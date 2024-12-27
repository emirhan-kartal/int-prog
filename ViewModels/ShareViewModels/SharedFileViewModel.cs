public class SharedFileViewModel
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string Token { get; set; }
    public DateTime ExpiryDate { get; set; }

    public string FormattedSize
    {
        get
        {
            if (Size < 1024) return $"{Size} B";
            if (Size < 1024 * 1024) return $"{Size / 1024.0:F2} KB";
            if (Size < 1024 * 1024 * 1024) return $"{Size / (1024.0 * 1024.0):F2} MB";
            return $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
} 