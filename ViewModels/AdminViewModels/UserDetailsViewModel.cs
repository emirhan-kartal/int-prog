

using ViewModels.FolderViewModels;

namespace ViewModels.AdminViewModels
{
    public class UserDetailsViewModel
    {
        public string UserId { get; set; }
        public string UserEmail { get; set; }
        public string FormattedStorageUsed { get; set; }
        public IEnumerable<FileViewModel> Files { get; set; }
        public IEnumerable<FolderViewModel> Folders { get; set; }
    }
} 