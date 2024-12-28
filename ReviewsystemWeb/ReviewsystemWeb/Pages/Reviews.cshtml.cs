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
using OfficeOpenXml;

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

        public async Task<IActionResult> OnGetAsync([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
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

                    // Filter reviews by date range if provided
                    if (fromDate.HasValue && toDate.HasValue)
                    {
                        AllReviews = AllReviews
                            .Where(r => r.CreatedAt.Date >= fromDate.Value.Date && r.CreatedAt.Date <= toDate.Value.Date)
                            .ToList();
                    }

                    // 1) Build a distribution for how many reviews per rating
                    RatingDistribution = AllReviews
                        .GroupBy(r => r.Rating)
                        .Select(g => new RatingCount { Rating = g.Key, Count = g.Count() })
                        .OrderBy(rc => rc.Rating)
                        .ToList();

                    // 2) Build monthly averages
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

                    foreach (var mg in monthlyGroups)
                    {
                        mg.Label = $"{mg.Year}-{mg.Month:D2}";
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
            return await OnGetAsync(null, null);
        }
        public async Task<IActionResult> OnPostDownloadSelectedExcelAsync(List<int> selectedReviews)
        {
            if (selectedReviews == null || !selectedReviews.Any())
            {
                Console.WriteLine("No reviews selected.");
                return RedirectToPage();
            }

            // Repopulate AllReviews if it's not already populated
            if (!AllReviews.Any())
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

                    AllReviews = reviews ?? new List<Review>();
                }
            }

            // Filter selected reviews
            var selectedData = AllReviews.Where(r => selectedReviews.Contains(r.ReviewId)).ToList();

            if (!selectedData.Any())
            {
                Console.WriteLine("No matching reviews found.");
                return RedirectToPage();
            }

            // Generate the Excel file
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Selected Reviews");

            // Add headers
            worksheet.Cells[1, 1].Value = "Review ID";
            worksheet.Cells[1, 2].Value = "Rating";
            worksheet.Cells[1, 3].Value = "Created At";
            worksheet.Cells[1, 4].Value = "Questions";

            // Add data
            for (int i = 0; i < selectedData.Count; i++)
            {
                var review = selectedData[i];
                worksheet.Cells[i + 2, 1].Value = review.ReviewId;
                worksheet.Cells[i + 2, 2].Value = review.Rating;
                worksheet.Cells[i + 2, 3].Value = review.CreatedAt.ToString("g");

                if (review.Questions != null && review.Questions.Count > 0)
                {
                    worksheet.Cells[i + 2, 4].Value = string.Join("\n", review.Questions.Select(q => $"{q.QuestionText} (Answer: {q.ResponseText})"));
                }
                else
                {
                    worksheet.Cells[i + 2, 4].Value = "No questions.";
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var memoryStream = new MemoryStream();
            package.SaveAs(memoryStream);
            memoryStream.Position = 0;

            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Selected_Reviews.xlsx");
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
