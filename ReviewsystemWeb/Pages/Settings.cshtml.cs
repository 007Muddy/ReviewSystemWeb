using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ReviewsystemWeb.Pages
{
    public class SettingsModel : PageModel
    {
        private readonly HttpClient _httpClient;

        public SettingsModel(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [BindProperty]
        public string Theme { get; set; } = "Normal"; // Default theme

        public string? ErrorMessage { get; set; }

        private const string ThemeApiUrl = "https://localhost:7006/api/theme";

        public async Task OnGetAsync()
        {
            try
            {
                string jwtToken = GetJwtTokenFromSession();
                if (string.IsNullOrEmpty(jwtToken))
                {
                    ErrorMessage = "User is not authenticated. Please log in.";
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                var response = await _httpClient.GetStringAsync(ThemeApiUrl);
                var themeResponse = JsonSerializer.Deserialize<ThemeResponse>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (themeResponse != null && !string.IsNullOrEmpty(themeResponse.Theme))
                {
                    Theme = themeResponse.Theme;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to load theme: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostUpdateThemeAsync()
        {
            try
            {
                string jwtToken = GetJwtTokenFromSession();
                if (string.IsNullOrEmpty(jwtToken))
                {
                    ErrorMessage = "User is not authenticated. Please log in.";
                    return RedirectToPage("/Login");
                }

                var themeRequest = new { Theme = Theme };
                var json = JsonSerializer.Serialize(themeRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);

                var response = await _httpClient.PostAsync(ThemeApiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["SuccessMessage"] = $"Theme changed to {Theme} successfully!";
                    return RedirectToPage();
                }

                ErrorMessage = $"Failed to update theme: {response.ReasonPhrase}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error updating theme: {ex.Message}";
            }

            return Page();
        }

        private string GetJwtTokenFromSession()
        {
            // Assuming the token is stored in the session with the key "JwtToken".
            return HttpContext.Session.GetString("JwtToken");
        }

        public class ThemeResponse
        {
            public string Theme { get; set; }
        }
    }
}
