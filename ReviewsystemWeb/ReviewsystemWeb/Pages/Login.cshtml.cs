using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ReviewsystemWeb.Pages
{
    public class LoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        private readonly IHttpClientFactory _httpClientFactory;

        public LoginModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Please fill in both fields";
                return Page();
            }

            var loginModel = new
            {
                Username = Username,
                Password = Password
            };

            var httpClient = _httpClientFactory.CreateClient();
            var json = JsonSerializer.Serialize(loginModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var loginUrl = "https://localhost:7006/api/auth/login";
                var response = await httpClient.PostAsync(loginUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(resultJson);

                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        HttpContext.Session.SetString("JwtToken", result.Token);

                        // Determine the role of the user
                        if (result.Role == "Admin")
                        {
                            return RedirectToPage("/AdminViewPage");
                        }
                        else
                        {
                            return RedirectToPage("/InspectionsListPage");
                        }
                    }
                    else
                    {
                        ErrorMessage = "Login failed: No token received.";
                        return Page();
                    }
                }
                else
                {
                    ErrorMessage = "Login failed: Username or password is incorrect.";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }

        public class LoginResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }  // The token field returned by the API

            [JsonPropertyName("role")]
            public string Role { get; set; }  // The role of the user, e.g., "Admin" or "User"
        }
    }
}
