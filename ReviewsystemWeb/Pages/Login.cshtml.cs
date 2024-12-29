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
                ErrorMessage = "Please fill in both fields.";
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
                        // Save the token in session storage
                        HttpContext.Session.SetString("JwtToken", result.Token);

                        // Determine the role of the user
                        return result.Role switch
                        {
                            "Admin" => RedirectToPage("/AdminViewPage"),
                            "User" => RedirectToPage("/Reviews"),
                            _ => HandleUnknownRole()
                        };
                    }
                    else
                    {
                        ErrorMessage = "Login failed: No token received.";
                        return Page();
                    }
                }
                else
                {
                    // Extract error details from the response
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    ErrorMessage = $"Login failed: {response.StatusCode} - {errorDetails}";
                    return Page();
                }
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = "Network error: Unable to connect to the server. Please check your internet connection.";
                return Page();
            }
            catch (JsonException ex)
            {
                ErrorMessage = "An error occurred while processing the server response.";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                return Page();
            }
        }

        private IActionResult HandleUnknownRole()
        {
            ErrorMessage = "Unknown role received from the server.";
            return Page();
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