using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ViewModels.AdminViewModels;
using Microsoft.EntityFrameworkCore;

namespace Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGenericRepository<FileEntity> _fileRepository;
        private readonly IGenericRepository<Folder> _folderRepository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            IGenericRepository<FileEntity> fileRepository,
            IGenericRepository<Folder> folderRepository,
            IHubContext<NotificationHub> hubContext,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _hubContext = hubContext;
            _roleManager = roleManager;
            _environment = environment;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        {
            try
            {
                var totalUsers = await _userManager.Users.CountAsync();
                var totalFiles = await _fileRepository.GetAllAsync();
                var totalStorage = await _userManager.Users.SumAsync(u => u.TotalStorageUsed);

                var viewModel = new AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    TotalFiles = totalFiles.Count(),
                    TotalStorageUsed = totalStorage,
                    RecentUsers = await _userManager.Users
                        .OrderByDescending(u => u.CreatedAt)
                        .Take(5)
                        .Select(u => new ViewModels.AdminViewModels.UserViewModel
                        {
                            Id = u.Id,
                            Email = u.Email,
                            CreatedAt = u.CreatedAt,
                            TotalStorageUsed = u.TotalStorageUsed
                        })
                        .ToListAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new AdminDashboardViewModel());
            }
        }
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var viewModels = new List<ViewModels.AdminViewModels.UserViewModel>();

                foreach (var u in users)
                {
                    viewModels.Add(new ViewModels.AdminViewModels.UserViewModel
                    {
                        Id = u.Id,
                        Email = u.Email,
                        CreatedAt = u.CreatedAt,
                        TotalStorageUsed = u.TotalStorageUsed,
                        IsAdmin = await _userManager.IsInRoleAsync(u, "Admin")
                    });
                }

                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users list");
                return View(new List<UserViewModel>());
            }
        }

        public async Task<IActionResult> UserFiles(string userId, int? folderId = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (folderId.HasValue)
            {
                var folder = await _folderRepository.GetByIdAsync(folderId.Value);
                if (folder == null || folder.UserId != userId)
                {
                    return NotFound();
                }
            }

            var files = await _fileRepository.GetAllAsync();
            var folders = await _folderRepository.GetAllAsync();

            string folderPath = "";
            if (folderId.HasValue)
            {
                var pathFolders = new List<string>();
                var currentFolder = await _folderRepository.GetByIdAsync(folderId.Value);
                while (currentFolder != null)
                {
                    pathFolders.Add(currentFolder.Name);
                    currentFolder = currentFolder.ParentFolderId.HasValue ?
                        await _folderRepository.GetByIdAsync(currentFolder.ParentFolderId.Value) : null;
                }
                pathFolders.Reverse();
                folderPath = "/" + string.Join("/", pathFolders);
            }

            ViewBag.CurrentFolderId = folderId;
            ViewBag.CurrentFolderPath = folderPath;

            var viewModel = new UserDetailsViewModel
            {
                UserId = userId,
                UserEmail = user.Email,
                FormattedStorageUsed = FormatBytes(user.TotalStorageUsed),
                Files = files.Where(f => f.UserId == userId && f.FolderId == folderId)
                            .Select(f => new FileViewModel
                            {
                                Id = f.Id,
                                FileName = f.OriginalFileName,
                                ContentType = f.ContentType,
                                Size = f.Size,
                                UploadedAt = f.UploadedAt,
                                FolderName = f.Folder?.Name,
                                FolderId = f.FolderId
                            }),
                Folders = folders.Where(f => f.UserId == userId && f.ParentFolderId == folderId)
                                .Select(f => new ViewModels.FolderViewModels.FolderViewModel
                                {
                                    Id = f.Id,
                                    Name = f.Name,
                                    CreatedAt = f.CreatedAt,
                                    FileCount = f.Files?.Count ?? 0
                                })
            };

            return View(viewModel);
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F2} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return BadRequest("Cannot delete admin users");
            }

            var files = await _fileRepository.GetAllAsync();
            var userFiles = files.Where(f => f.UserId == userId);
            foreach (var file in userFiles)
            {
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.FileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            await _userManager.DeleteAsync(user);
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(string email, string password)
        {
            try
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    await _hubContext.Clients.All
                        .SendAsync("ReceiveNotification", $"New user {email} created");
                    return Json(new { success = true });
                }

                return Json(new { success = false, errors = result.Errors.Select(e => e.Description) });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return Json(new { success = false, errors = new[] { "An error occurred while creating the user." } });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditUser(string userId, string email, string password)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            if (!string.IsNullOrEmpty(email) && user.Email != email)
            {
                user.Email = email;
                user.UserName = email;
                await _userManager.UpdateAsync(user);
            }

            if (!string.IsNullOrEmpty(password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _userManager.ResetPasswordAsync(user, token, password);
            }

            await _hubContext.Clients.All
                .SendAsync("ReceiveNotification", $"User {email} updated");

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> ModifyFile(int fileId, string newName)
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
                return NotFound();

            file.OriginalFileName = newName;
            await _fileRepository.UpdateAsync(file);

            await _hubContext.Clients.User(file.UserId)
                .SendAsync("ReceiveNotification", "An admin modified your file");

            return RedirectToAction(nameof(UserFiles), new { userId = file.UserId });
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(string userId, IFormFile file, int? folderId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();
            long limit = 10L * 1024 * 1024 * 1024*1024;
            if (user.TotalStorageUsed + file.Length > limit)
            {
                return BadRequest("Storage limit exceeded");
            }

            var fileEntity = await UploadFile(file, user.Id, folderId);

            user.TotalStorageUsed += file.Length;
            await _userManager.UpdateAsync(user);

            await _hubContext.Clients.User(user.Id)
                .SendAsync("ReceiveNotification", "Admin uploaded a file to your account");

            return RedirectToAction(nameof(UserFiles), new { userId, folderId });
        }

        private async Task<FileEntity> UploadFile(IFormFile file, string userId, int? folderId)
        {
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(_environment.WebRootPath, "uploads", uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var fileEntity = new FileEntity
            {
                FileName = uniqueFileName,
                OriginalFileName = file.FileName,
                ContentType = file.ContentType,
                Size = file.Length,
                UserId = userId,
                FolderId = folderId
            };

            return await _fileRepository.AddAsync(fileEntity);
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder(string userId, string folderName, int? parentFolderId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var folder = new Folder
            {
                Name = folderName,
                UserId = userId,
                ParentFolderId = parentFolderId,
                CreatedAt = DateTime.UtcNow
            };

            await _folderRepository.AddAsync(folder);

            await _hubContext.Clients.User(userId)
                .SendAsync("ReceiveNotification", "Admin created a folder in your account");

            return RedirectToAction(nameof(UserFiles), new { userId, folderId = parentFolderId });
        }
    }
}