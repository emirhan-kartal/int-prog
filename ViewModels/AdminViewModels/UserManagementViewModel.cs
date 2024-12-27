using System.ComponentModel.DataAnnotations;
using ViewModels.FolderViewModels;
namespace AdminViewModels
{
    public class UserManagementViewModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public long TotalStorageUsed { get; set; }
        public bool IsAdmin { get; set; }

        public string StorageUsedFormatted =>
            $"{TotalStorageUsed / (1024.0 * 1024.0 * 1024.0):F2} GB";

        public string CreatedAtFormatted =>
            CreatedAt.ToString("MM/dd/yyyy HH:mm");
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Is Admin")]
        public bool IsAdmin { get; set; }
    }

    public class UserDetailsViewModel
    {
        public UserManagementViewModel User { get; set; }
        public FileListViewModel Files { get; set; }
        public FolderListViewModel Folders { get; set; }
    }
}