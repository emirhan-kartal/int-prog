using System;
using System.ComponentModel.DataAnnotations;

namespace ViewModels.FolderViewModels
{
    public class FolderViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Folder name is required")]
        [StringLength(50, ErrorMessage = "Folder name cannot be longer than 50 characters")]
        [RegularExpression(@"^[^<>:;/\\\|\?\*""]+$",
            ErrorMessage = "Folder name contains invalid characters")]
        [Display(Name = "Folder Name")]
        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }
        public int FileCount { get; set; }
        public int? ParentFolderId { get; set; }
    }

    public class FolderListViewModel
    {
        public IEnumerable<FolderViewModel> Folders { get; set; }
        public FolderViewModel NewFolder { get; set; }
    }
}