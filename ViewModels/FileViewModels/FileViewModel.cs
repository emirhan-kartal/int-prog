using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using ViewModels.FolderViewModels;

public class FileViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
    public string FolderName { get; set; }
    public int? FolderId { get; set; }
    
    // Helper property to format file size
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

public class FileUploadViewModel
{
    [Required(ErrorMessage = "Please select a file")]
    public IFormFile File { get; set; }
    
    public int? FolderId { get; set; }
    
    [Display(Name = "Folder")]
    public string FolderName { get; set; }
    
    public List<SelectListItem> AvailableFolders { get; set; }
}

public class FileListViewModel
{
    public IEnumerable<FileViewModel> Files { get; set; }
    public IEnumerable<FolderViewModel> Folders { get; set; }
    public long TotalStorageUsed { get; set; }
    public long StorageLimit { get; set; } = 10L * 1024 * 1024 * 1024; // 10GB in bytes
    public int? CurrentFolderId { get; set; }
    public int? ParentFolderId { get; set; }
    public string CurrentFolderPath { get; set; }

    public double StorageUsedPercentage
    {
        get
        {
            if (StorageLimit == 0) return 0;
            return Math.Min(100, ((double)TotalStorageUsed / StorageLimit) * 100);
        }
    }

    public string StorageUsedFormatted
    {
        get
        {
            var usedGB = TotalStorageUsed / (1024.0 * 1024.0 * 1024.0);
            var limitGB = StorageLimit / (1024.0 * 1024.0 * 1024.0);
            return $"{usedGB:F2} GB / {limitGB:F0} GB";
        }
    }
}