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
                        var url = $"https://localhost:7085/api/analytics/revenue?start={start:O}&end={end:O}";
                        var revenue = await GetAsync<RevenueResponseDto>(client, url);
                        var answer = $"Revenue from {start:yyyy-MM-dd} to {end:yyyy-MM-dd} is {revenue.Total:N0} {revenue.Currency}.";
                        var aiAnswer = await AskGeminiAsync($"Summarize this result: {answer}");
                        return Ok(new ChatResponseDto(aiAnswer, new { asnwer = answer }));
                    }
                case "top-products": {
                        var url = $"https://localhost:7085/api/analytics/top-products?start={start:O}&end={end:O}&take={take}";
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

        //private async Task<string> AskGeminiAsync(string prompt)
        //{
        //    var apiKey = _config["Gemini:ApiKey"];
        //    if (string.IsNullOrEmpty(apiKey))
        //        throw new Exception("Gemini API key not found");

        //    using var client = new HttpClient();

        //    var requestBody = new {
        //        contents = new[]
        //        {
        //    new { parts = new[] { new { text = prompt } } }
        //}
        //    };

        //    var json = JsonSerializer.Serialize(requestBody);

        //    var request = new HttpRequestMessage(HttpMethod.Post,
        //        "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-preview:generateContent");

        //    request.Headers.Add("x-goog-api-key", apiKey);
        //    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        //    var response = await client.SendAsync(request);
        //    if (response.StatusCode == (HttpStatusCode)429) {
        //        await Task.Delay(2000);
        //    }
        //    response.EnsureSuccessStatusCode();

        //    var responseJson = await response.Content.ReadAsStringAsync();
        //    var result = JsonDocument.Parse(responseJson);

        //    return result.RootElement
        //        .GetProperty("candidates")[0]
        //        .GetProperty("content")
        //        .GetProperty("parts")[0]
        //        .GetProperty("text")
        //        .GetString();
        //}

        public async Task<string> AskClaudeAsync(string prompt)
        {
            var apiKey = _config["Claude:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new Exception("Claude API key not found");

            var requestBody = new {
                model = "claude-3-opus-20240229", // or claude-3-sonnet, claude-3-haiku
                max_tokens = 500,
                messages = new[]
                {
                new { role = "user", content = prompt }
            }
            };

            var json = JsonSerializer.Serialize(requestBody);

            var request = new HttpRequestMessage(HttpMethod.Post,
                "https://api.anthropic.com/v1/messages");

            request.Headers.Add("x-api-key", apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var client = new HttpClient();
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonDocument.Parse(responseJson);

            return result.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString();
        }


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