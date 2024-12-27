using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using static ReviewsystemWeb.Models.ReviewModel;

namespace ReviewsystemWeb.Pages
{
    public class ReviewsModel : PageModel
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ReviewsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public List<Review> AllReviews { get; set; } = new();

        // Holds distribution of reviews by rating (for bar chart)
        public List<RatingCount> RatingDistribution { get; set; } = new();

        // Holds average rating by month (for line chart)
        public List<MonthAverage> MonthlyAverages { get; set; } = new();

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

                    // 1) Build a distribution for how many reviews per rating
                    RatingDistribution = AllReviews
                        .GroupBy(r => r.Rating)
                        .Select(g => new RatingCount { Rating = g.Key, Count = g.Count() })
                        .OrderBy(rc => rc.Rating)
                        .ToList();

                    // 2) Build monthly averages
                    //    Group reviews by (month, year) or just month
                    //    Calculate average rating
                    var monthlyGroups = AllReviews
                        .GroupBy(r => new { r.CreatedAt.Year, r.CreatedAt.Month })
                        .Select(g => new MonthAverage
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            AverageRating = g.Average(r => r.Rating)
                        })
                        .OrderBy(ma => ma.Year)
                        .ThenBy(ma => ma.Month)
                        .ToList();

                    // Convert (Year, Month) to something like "2023-08" or a friendly string
                    foreach (var mg in monthlyGroups)
                    {
                        mg.Label = $"{mg.Year}-{mg.Month.ToString("D2")}";
                    }

                    MonthlyAverages = monthlyGroups;
                }
            }
            // else handle errors as needed

            return Page();
        }
        public async Task<IActionResult> OnPostDeleteSelectedAsync(List<int> selectedReviews)
        {
            if (selectedReviews == null || !selectedReviews.Any())
            {
                // No reviews selected
                return RedirectToPage();
            }

            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToPage("/Login");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            foreach (var reviewId in selectedReviews)
            {
                var apiUrl = $"https://localhost:7006/api/Reviews/{reviewId}";
                await client.DeleteAsync(apiUrl);
            }

            // Refresh the list of reviews
            return await OnGetAsync();
        }




        public class RatingCount
        {
            public int Rating { get; set; }
            public int Count { get; set; }
        }

        public class MonthAverage
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public double AverageRating { get; set; }

            public string Label { get; set; } = "";
        }
    }
}
