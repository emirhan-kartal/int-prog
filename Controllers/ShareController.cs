using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class ShareController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly IGenericRepository<FileEntity> _fileRepository;
    private readonly ILogger<ShareController> _logger;

    public ShareController(
        IConfiguration configuration,
        IGenericRepository<FileEntity> fileRepository,
        ILogger<ShareController> logger)
    {
        _configuration = configuration;
        _fileRepository = fileRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string token)
    {
        try
        {
            var (fileId, expiryDate) = DecodeShareToken(token);

            if (expiryDate < DateTime.UtcNow)
            {
                return BadRequest("Share link has expired");
            }

            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return NotFound("File not found");
            }

            var viewModel = new SharedFileViewModel
            {
                FileName = file.OriginalFileName,
                ContentType = file.ContentType,
                Size = file.Size,
                Token = token,
                ExpiryDate = expiryDate
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding share token");
            return BadRequest("Invalid share link");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Download(string token)
    {
        try
        {
            var (fileId, expiryDate) = DecodeShareToken(token);

            if (expiryDate < DateTime.UtcNow)
            {
                return BadRequest("Share link has expired");
            }

            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return NotFound("File not found");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", file.FileName);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("File not found");
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, file.ContentType, file.OriginalFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file");
            return BadRequest("Error downloading file");
        }
    }

    private (int fileId, DateTime expiryDate) DecodeShareToken(string token)
    {
        try
        {
            // Get the key from configuration
            string configKey = _configuration["ShareTokenKey"].Substring(0,32);
            byte[] keyBytes = new byte[32];
            Array.Copy(Encoding.UTF8.GetBytes(configKey), keyBytes, Math.Min(configKey.Length, 32));

            // Create decryption objects
            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.IV = new byte[16];

            // Restore Base64 string from URL-safe format
            string base64 = token
                .Replace('-', '+')
                .Replace('_', '/');

            // Add padding if needed
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            byte[] cipherBytes = Convert.FromBase64String(base64);

            // Decrypt
            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            string plainText = Encoding.UTF8.GetString(plainBytes);

            // Parse the decrypted string
            string[] parts = plainText.Split('|');
            if (parts.Length != 2)
                throw new FormatException("Invalid token format");

            int fileId = int.Parse(parts[0]);
            DateTime expiryDate = DateTime.ParseExact(parts[1], "yyyyMMddHHmmss", null);

            return (fileId, expiryDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decoding share token");
            throw;
        }
    }
}