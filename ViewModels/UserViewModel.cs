public class UserViewModel
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public long TotalStorageUsed { get; set; }
    public string StorageUsedFormatted => 
        (TotalStorageUsed / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GB";
} 