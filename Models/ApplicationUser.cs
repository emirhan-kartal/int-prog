using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long TotalStorageUsed { get; set; } // in bytes
    public virtual ICollection<FileEntity> Files { get; set; }
    public virtual ICollection<Folder> Folders { get; set; }
} 