using FileStoringService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace FileStoringService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;

        private static string _computeFileHash(Stream stream)
        {
            using var sha = SHA256.Create();
            var hashBytes = sha.ComputeHash(stream);
            return Convert.ToHexString(hashBytes); // .NET 5+; for older: BitConverter.ToString(...)
        }

        public StorageController(IWebHostEnvironment env, AppDbContext db)
        {
            _env = env;
            _db = db;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided");

            // 1. Compute hash
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            var hash = _computeFileHash(memoryStream);

            // 2. Check if already exists
            var existing = await _db.FileRecords.FirstOrDefaultAsync(f => f.Hash == hash);
            if (existing != null)
            {
                return Ok(new { message = "File already exists", existing.Id });
            }

            // 3. Save file
            memoryStream.Position = 0; // Reset for writing to disk
            var uploads = Path.Combine(_env.ContentRootPath, "UploadedFiles");
            Directory.CreateDirectory(uploads);
            var filePath = Path.Combine(uploads, file.FileName);
            using (var stream = System.IO.File.Create(filePath))
            {
                await memoryStream.CopyToAsync(stream);
            }

            // 4. Save to DB
            var record = new FileRecord
            {
                FileName = file.FileName,
                FilePath = filePath,
                Hash = hash
            };

            _db.FileRecords.Add(record);
            await _db.SaveChangesAsync();

            return Ok(new { message = "New file saved", record.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFile(int id)
        {
            var record = await _db.FileRecords.FindAsync(id);
            if (record == null) return NotFound();

            var stream = new FileStream(record.FilePath, FileMode.Open, FileAccess.Read);
            return File(stream, "application/octet-stream", record.FileName);
        }
    }
}
