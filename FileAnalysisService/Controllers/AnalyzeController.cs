using FileAnalysisService.Models;
using FileAnalysisService.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FileAnalysisService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AnalyzeController(IWebHostEnvironment env, AppDbContext db, IHttpClientFactory httpClientFactory) : ControllerBase
    {
        [HttpGet("{fileId}")]
        public async Task<IActionResult> AnalyzeFile(int fileId)
        {
            var client = httpClientFactory.CreateClient();

            // Call FileStoringService internally
            var response = await client.GetAsync($"http://filestoring/api/storage/{fileId}");
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Could not retrieve file.");
            }

            // Read stream as text
            using var textStream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(textStream);
            var text = await reader.ReadToEndAsync();

            // Get file name from Content-Disposition header
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"file_{fileId}.txt";


            var json = JsonSerializer.Serialize(new
            {
                format = "png",
                width = 1000,
                height = 1000,
                fontScale = 15,
                scale = "linear",
                removeStopwords = true,
                minWordLength = 4,
                text
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var cloudResponse = await client.PostAsync("https://quickchart.io/wordcloud", content);

            // Get image as stream
            var imageStream = await cloudResponse.Content.ReadAsStreamAsync();

            // Save to file
            imageStream.Position = 0; // Reset for writing to disk
            var imageName = fileName.Split('.')[0] + ".png";
            var uploads = Path.Combine(env.ContentRootPath, "Files");
            Directory.CreateDirectory(uploads);
            var imagePath = Path.Combine(uploads, imageName);
            using (var fileStream = System.IO.File.Create(imagePath))
            {
                await imageStream.CopyToAsync(fileStream);
            }

            // Save to DB
            var existing = await db.WordCloudRecords.FindAsync(fileId);

            if (existing is null)
            {
                // Insert new record
                var record = new WordCloudRecord
                {
                    Id = fileId,
                    FileName = imageName,
                    FilePath = imagePath
                };
                db.WordCloudRecords.Add(record);
            }
            else
            {
                // Update existing record
                existing.FileName = imageName;
                existing.FilePath = imagePath;
                db.WordCloudRecords.Update(existing); // optional, EF tracks changes
            }

            await db.SaveChangesAsync();

            // Analyze the text
            var analysis = TextAnalyzer.Analyze(text);

            return Ok(new
            {
                message = $"File {fileName} succesfully analyzed",
                analysis.ParagraphCount,
                analysis.WordCount,
                analysis.SymbolCount
            });
        }

        [HttpGet("wordcloud/{id}")]
        public async Task<IActionResult> GetFile(int id)
        {
            var record = await db.WordCloudRecords.FindAsync(id);
            if (record == null) return NotFound();

            var stream = new FileStream(record.FilePath, FileMode.Open, FileAccess.Read);
            return File(stream, "application/octet-stream", record.FileName);
        }
    }
}
