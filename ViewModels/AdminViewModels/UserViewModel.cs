using System;

namespace ViewModels.AdminViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public long TotalStorageUsed { get; set; }
        public bool IsAdmin { get; set; }
        
        public string FormattedStorageUsed
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