using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ReviewsystemWeb.Pages
{
    public class AdminViewPageModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public List<User> Users { get; set; } = new List<User>();

        private readonly IHttpClientFactory _httpClientFactory;

        public AdminViewPageModel(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRegisterUserAsync()
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ModelState.AddModelError(string.Empty, "Please fill in all fields");
                await LoadUsersAsync(); // Reload users to keep data on page
                return Page();
            }

            var registerModel = new
            {
                Username,
                Email,
                Password
            };

            var httpClient = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var json = JsonSerializer.Serialize(registerModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://localhost:7006/api/auth/register", content);
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "User registered successfully";
                return RedirectToPage(); // Refresh the page to update the list of users
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"User registration failed: {errorMessage}");
                await LoadUsersAsync(); // Reload users to keep data on page
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(string usernameToDelete)
        {
            if (string.IsNullOrEmpty(usernameToDelete))
            {
                ModelState.AddModelError(string.Empty, "Invalid user selected for deletion");
                await LoadUsersAsync();
                return Page();
            }

            var httpClient = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.DeleteAsync($"https://localhost:7006/api/auth/delete/{usernameToDelete}");
            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "User deleted successfully";
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to delete user: {errorMessage}");
            }

            return RedirectToPage(); // Refresh the page to update the list of users
        }

        private async Task LoadUsersAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JwtToken");
            if (!string.IsNullOrEmpty(token))
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            }

            var response = await httpClient.GetAsync("https://localhost:7006/api/auth/users");
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                Users = JsonSerializer.Deserialize<List<User>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                var errorMessage = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Failed to fetch users: {errorMessage}");
            }
        }

        public class User
        {
            [JsonPropertyName("userName")]
            public string UserName { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }
        }
    }
}
