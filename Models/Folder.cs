public class Folder
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? ParentFolderId { get; set; }
    public virtual Folder ParentFolder { get; set; }
    public virtual ApplicationUser User { get; set; }
    public virtual ICollection<FileEntity> Files { get; set; }
    public virtual ICollection<Folder> SubFolders { get; set; }
}