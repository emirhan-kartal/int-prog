using System.Collections.Generic;

namespace ViewModels.AdminViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalFiles { get; set; }
        public long TotalStorageUsed { get; set; }
        public IEnumerable<UserViewModel> RecentUsers { get; set; } = new List<UserViewModel>();

        public string FormattedTotalStorage
        {
            get
            {
                if (TotalStorageUsed < 1024) return $"{TotalStorageUsed} B";
                if (TotalStorageUsed < 1024 * 1024) return $"{TotalStorageUsed / 1024.0:F2} KB";
                if (TotalStorageUsed < 1024 * 1024 * 1024) return $"{TotalStorageUsed / (1024.0 * 1024.0):F2} MB";
                return $"{TotalStorageUsed / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
        }
    }
} 