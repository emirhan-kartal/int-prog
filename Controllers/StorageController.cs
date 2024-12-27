using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private const long MAX_STORAGE = 10L * 1024 * 1024 * 1024; // 10GB

        public StorageController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                used = user.TotalStorageUsed,
                total = MAX_STORAGE
            });
        }
    }
} 