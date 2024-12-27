using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using ViewModels.FolderViewModels;
using Microsoft.Extensions.Logging;

namespace Controllers
{
    [Authorize]
    public class FoldersController : Controller
    {
        private readonly IGenericRepository<Folder> _folderRepository;
        private readonly IGenericRepository<FileEntity> _fileRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<FoldersController> _logger;

        public FoldersController(
            IGenericRepository<Folder> folderRepository,
            IGenericRepository<FileEntity> fileRepository,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext,
            ILogger<FoldersController> logger)
        {
            _folderRepository = folderRepository;
            _userManager = userManager;
            _hubContext = hubContext;
            _logger = logger;
            _fileRepository = fileRepository;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var folders = await _folderRepository.GetAllAsync();
            var userFolders = folders.Where(f => f.UserId == user.Id)
                                   .Select(f => new FolderViewModel
                                   {
                                       Id = f.Id,
                                       Name = f.Name,
                                       CreatedAt = f.CreatedAt,
                                       FileCount = f.Files?.Count ?? 0
                                   });

            var viewModel = new FolderListViewModel
            {
                Folders = userFolders,
                NewFolder = new FolderViewModel()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] FolderViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.GetUserAsync(User);

                var folders = await _folderRepository.GetAllAsync();
                if (folders.Any(f => f.UserId == user.Id && f.Name == model.Name))
                {
                    return BadRequest("A folder with this name already exists");
                }

                var folder = new Folder
                {
                    Name = model.Name,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _folderRepository.AddAsync(folder);
                await _hubContext.Clients.User(user.Id)
                    .SendAsync("ReceiveNotification", "Folder created successfully!");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating folder");
                return BadRequest("Failed to create folder");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var folder = await _folderRepository.GetByIdAsync(id);
                var user = await _userManager.GetUserAsync(User);

                if (folder == null || folder.UserId != user.Id && !User.IsInRole("Admin"))
                {
                    return NotFound();
                }

                var files = await _fileRepository.GetAllAsync();
                if (files.Any(f => f.FolderId == id))
                {
                    return BadRequest("Cannot delete folder that contains files. Please delete files first.");
                }

                await _folderRepository.DeleteAsync(folder);

                await _hubContext.Clients.User(user.Id)
                    .SendAsync("ReceiveNotification", "Folder deleted successfully!");

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting folder");
                return BadRequest("Failed to delete folder");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Rename(int id, string newName)
        {
            var folder = await _folderRepository.GetByIdAsync(id);
            var user = await _userManager.GetUserAsync(User);

            if (folder == null || folder.UserId != user.Id && !User.IsInRole("Admin"))
            {
                return NotFound();
            }

            folder.Name = newName;
            await _folderRepository.UpdateAsync(folder);
            await _hubContext.Clients.User(user.Id)
                .SendAsync("ReceiveNotification", "Folder renamed successfully!");

            return RedirectToAction(nameof(Index));
        }
    }
}