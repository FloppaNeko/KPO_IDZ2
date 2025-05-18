using Microsoft.AspNetCore.Mvc;

namespace APIGateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GatewayController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public GatewayController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var client = _httpClientFactory.CreateClient();

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
            {
                Name = "file",
                FileName = file.FileName
            };
            content.Add(fileContent, "file", file.FileName);

            var response = await client.PostAsync("http://filestoring/api/storage/upload", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            return Content(responseContent, response.Content.Headers.ContentType?.MediaType ?? "application/json");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Download(int id)
        {
            var client = _httpClientFactory.CreateClient();

            var response = await client.GetAsync($"http://filestoring/api/storage/{id}");
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
            }

            var stream = await response.Content.ReadAsStreamAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? $"file_{id}";

            return File(stream, contentType, fileName);
        }
    }
}