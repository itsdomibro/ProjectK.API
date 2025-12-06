using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectK.API.Controllers
{
    public record ChatRequestDto(string Question, DateTime? Start = null, DateTime? End = null, int? Take = null);
    public record ChatResponseDto(string Answer, object? Data);

    [Route("api/chat")]
    [Authorize(Roles = "Owner")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ChatController(IHttpClientFactory httpClientFactory, LinkGenerator linkGenerator, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequestDto req)
        {
            var ownerIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(ownerIdStr, out _)) return Unauthorized();

            var (intent, start, end, take) = DetectIntent(req);

            var client = _httpClientFactory.CreateClient();
            var token = Request.Headers["Authorization"].ToString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Replace("Bearer ", ""));

            switch (intent) {
                case "revenue": {
                        //7085 port and https ( production by jeff need http and 5034 port for local dev)
                        var url = $"http://localhost:5034/api/analytics/revenue?start={start:O}&end={end:O}";
                        var revenue = await GetAsync<RevenueResponseDto>(client, url);
                        var answer = $"Revenue from {start:yyyy-MM-dd} to {end:yyyy-MM-dd} is {revenue.Total:N0} {revenue.Currency}.";
                        var aiAnswer = await AskGeminiAsync($"Summarize this result: {answer}");
                        return Ok(new ChatResponseDto(aiAnswer, new { asnwer = answer }));
                    }
                case "top-products": {
                        var url = $"http://localhost:5034/api/analytics/top-products?start={start:O}&end={end:O}&take={take}";
                        var top = await GetAsync<List<TopProductDto>>(client, url);
                        var summary = string.Join(", ", top.Select(p => $"{p.Name} ({p.Revenue:N0})"));
                        var answer = $"Top {take} products: {summary}.";
                        var aiAnswer = await AskGeminiAsync($"Summarize this result: {answer}");
                        return Ok(new ChatResponseDto(aiAnswer, new { asnwer = answer }));
                    }
                default:
                    return BadRequest("Sorry, I couldn't understand the question. Try asking about revenue or top products.");
            }
        }

        private async Task<string> AskGeminiAsync(string prompt)
        {
            var apiKey = _config["Gemini:ApiKey"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";

            var body = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = new HttpClient();

            var resp = await client.PostAsync(url, content);
            resp.EnsureSuccessStatusCode();

            var respJson = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(respJson);

            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()!;
        
        private static async Task<T> GetAsync<T>(HttpClient client, string url)
        {
            var resp = await client.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true
            })!;
        }

        private static (string intent, DateTime start, DateTime end, int take) DetectIntent(ChatRequestDto req)
        {
            var q = (req.Question ?? string.Empty).ToLowerInvariant();
            var end = req.End ?? DateTime.UtcNow;
            var start = req.Start ?? end.AddDays(-7);
            var take = req.Take ?? 5;

            if (q.Contains("revenue") || q.Contains("sales") || q.Contains("income"))
                return ("revenue", start, end, take);

            if (q.Contains("top") && q.Contains("product"))
                return ("top-products", start, end, take);

            return ("unknown", start, end, take);
        }
    }
}