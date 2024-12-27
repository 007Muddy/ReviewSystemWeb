using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ReviewsystemWeb.Pages
{
    public class ReviewsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Holds the fetched reviews
        public List<Review> AllReviews { get; set; } = new();

        // Holds a simple list of { Rating, Count } for the chart
        public List<RatingCount> RatingDistribution { get; set; } = new();

        public ReviewsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = "https://localhost:7006/api/Reviews";
            var response = await client.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var reviews = JsonSerializer.Deserialize<List<Review>>(
                    jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (reviews != null)
                {
                    AllReviews = reviews;

                    // Build a distribution for how many reviews per rating
                    // E.g., Count how many times rating 1, rating 2, etc.
                    RatingDistribution = AllReviews
                        .GroupBy(r => r.Rating)
                        .Select(g => new RatingCount { Rating = g.Key, Count = g.Count() })
                        .OrderBy(rc => rc.Rating)
                        .ToList();
                }
            }
            else
            {
                // Handle error if needed
            }

            return Page();
        }

        public class Review
        {
            public int ReviewId { get; set; }
            public int Rating { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<Question> Questions { get; set; } = new();
        }

        public class Question
        {
            public int QuestionId { get; set; }
            public string QuestionText { get; set; }
            public string ResponseText { get; set; }
            public int ReviewId { get; set; }
        }

        // Helper class for rating distribution
        public class RatingCount
        {
            public int Rating { get; set; }
            public int Count { get; set; }
        }
    }
}
