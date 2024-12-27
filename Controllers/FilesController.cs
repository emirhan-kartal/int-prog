using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Collections.Generic;
using ViewModels.FolderViewModels;

namespace Controllers
{
    [Authorize]
    public class FilesController : Controller
    {
        private readonly IGenericRepository<FileEntity> _fileRepository;
        private readonly IGenericRepository<Folder> _folderRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FilesController> _logger;
        private const long MAX_STORAGE = 10L * 1024 * 1024 * 1024; // 10GB in bytes
        private readonly SignInManager<ApplicationUser> _signInManager;

        public FilesController(
            IGenericRepository<FileEntity> fileRepository,
            IGenericRepository<Folder> folderRepository,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            IHubContext<NotificationHub> hubContext,
            IConfiguration configuration,
            ILogger<FilesController> logger,
            SignInManager<ApplicationUser> signInManager)

        {
            _fileRepository = fileRepository;
            _folderRepository = folderRepository;
            _userManager = userManager;
            _environment = environment;
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
            _signInManager = signInManager;

        }

        public async Task<IActionResult> Index(int? folderId)
        {
            var user = await _userManager.GetUserAsync(User);
            var files = await _fileRepository.GetAllAsync();
            var folders = await _folderRepository.GetAllAsync();
            if (user == null)
            {
                _logger.LogWarning("User not found while accessing Files/Index. Signing out.");
                await _signInManager.SignOutAsync();
                return RedirectToAction("Index", "Home");
            }
            string folderPath = "";
            if (folderId.HasValue)
            {
                var pathFolders = new List<string>();
                var currentFolderPath = await _folderRepository.GetByIdAsync(folderId.Value);
                while (currentFolderPath != null)
                {
                    pathFolders.Add(currentFolderPath.Name);
                    currentFolderPath = currentFolderPath.ParentFolderId.HasValue ?
                        await _folderRepository.GetByIdAsync(currentFolderPath.ParentFolderId.Value) : null;
                }
                pathFolders.Reverse();
                folderPath = "/" + string.Join("/", pathFolders);
            }

            var userFiles = files.Where(f => f.UserId == user.Id)
                                .Select(f => new FileViewModel
                                {
                                    Id = f.Id,
                                    FileName = f.OriginalFileName,
                                    ContentType = f.ContentType,
                                    Size = f.Size,
                                    UploadedAt = f.UploadedAt,
                                    FolderName = f.Folder?.Name,
                                    FolderId = f.FolderId
                                });

            var userFolders = folders.Where(f => f.UserId == user.Id)
                                    .Select(f => new FolderViewModel
                                    {
                                        Id = f.Id,
                                        Name = f.Name,
                                        CreatedAt = f.CreatedAt,
                                        FileCount = f.Files?.Count ?? 0,
                                        ParentFolderId = f.ParentFolderId
                                    });

            var currentFolder = folderId.HasValue ?
                await _folderRepository.GetByIdAsync(folderId.Value) : null;

            var viewModel = new FileListViewModel
            {
                Files = userFiles,
                Folders = userFolders,
                TotalStorageUsed = userFiles.Sum(f => f.Size),
                StorageLimit = MAX_STORAGE,
                CurrentFolderId = folderId,
                ParentFolderId = currentFolder?.ParentFolderId,
                CurrentFolderPath = folderPath
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] FileUploadViewModel model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                long MAX_STORAGE = 10l * 1024 * 1024*1024;
                if (user.TotalStorageUsed + model.File.Length > MAX_STORAGE)
                {
                    await _hubContext.Clients.User(user.Id)
                        .SendAsync("ReceiveNotification", "Storage limit exceeded!");
                    return BadRequest("Storage limit exceeded");
                }

                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.File.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.File.CopyToAsync(stream);
                }

                var fileEntity = new FileEntity
                {
                    FileName = uniqueFileName,
                    OriginalFileName = model.File.FileName,
                    ContentType = model.File.ContentType,
                    Size = model.File.Length,
                    UserId = user.Id,
                    FolderId = model.FolderId
                };

                await _fileRepository.AddAsync(fileEntity);

                user.TotalStorageUsed += model.File.Length;
                await _userManager.UpdateAsync(user);

                await _hubContext.Clients.User(user.Id)
                    .SendAsync("ReceiveNotification", "File uploaded successfully!");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            var user = await _userManager.GetUserAsync(User);

            if (file == null || file.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return NotFound();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.FileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            await _fileRepository.DeleteAsync(file);

            user.TotalStorageUsed -= file.Size;
            await _userManager.UpdateAsync(user);

            await _hubContext.Clients.User(user.Id)
                .SendAsync("ReceiveNotification", "File deleted successfully!");

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Download(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            var user = await _userManager.GetUserAsync(User);

            if (file == null || file.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return NotFound();
            }

            var filePath = Path.Combine(_environment.WebRootPath, "uploads", file.FileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, file.ContentType, file.OriginalFileName);
        }

        [HttpPost]
        public async Task<IActionResult> Share(int id)
        {
            var file = await _fileRepository.GetByIdAsync(id);
            var user = await _userManager.GetUserAsync(User);

            if (file == null || file.UserId != user.Id)
                return NotFound();

            var token = GenerateShareToken(id);
            if (token == null)
                return BadRequest("Could not generate share link");

            var shareUrl = Url.Action("Index", "Share",
                new { token = token }, Request.Scheme);

            return Json(new { url = shareUrl });
        }

        private string GenerateShareToken(int fileId)
        {
            try
            {
                // Get the key from configuration and ensure it's 32 bytes (256 bits)
                string configKey = _configuration["ShareTokenKey"].Substring(0, 32);

                var iv = new byte[16];
                using var aes = Aes.Create();
                aes.Key = Encoding.UTF8.GetBytes(configKey);
                aes.IV = iv;

                var plainText = $"{fileId}|{DateTime.UtcNow.AddDays(7):yyyyMMddHHmmss}";
                var plainBytes = Encoding.UTF8.GetBytes(plainText);

                using var encryptor = aes.CreateEncryptor();
                var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                // Make the Base64 string URL-safe
                return Convert.ToBase64String(encryptedBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating share token");
                return null;
            }
        }
    }
}