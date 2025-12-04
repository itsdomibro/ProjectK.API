using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

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

        public ChatController(IHttpClientFactory httpClientFactory, LinkGenerator linkGenerator)
        {
            _httpClientFactory = httpClientFactory;
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
                        var url = $"https://localhost:5001/api/analytics/revenue?start={start:O}&end={end:O}";
                        var revenue = await GetAsync<RevenueResponseDto>(client, url);
                        var answer = $"Revenue from {start:yyyy-MM-dd} to {end:yyyy-MM-dd} is {revenue.Total:N0} {revenue.Currency}.";
                        return Ok(new ChatResponseDto(answer, revenue));
                    }
                case "top-products": {
                        var url = $"https://localhost:5001/api/analytics/top-products?start={start:O}&end={end:O}&take={take}";
                        var top = await GetAsync<List<TopProductDto>>(client, url);
                        var summary = string.Join(", ", top.Select(p => $"{p.Name} ({p.Revenue:N0})"));
                        var answer = $"Top {take} products: {summary}.";
                        return Ok(new ChatResponseDto(answer, top));
                    }
                default:
                    return BadRequest("Sorry, I couldn't understand the question. Try asking about revenue or top products.");
            }
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