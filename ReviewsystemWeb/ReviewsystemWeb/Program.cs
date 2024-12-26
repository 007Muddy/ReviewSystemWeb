var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSession(); // Add session support
builder.Services.AddHttpClient(); // Register HttpClient
builder.Services.AddAuthentication(); // If you have authentication configured

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();


app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}"); // Set login as default
    endpoints.MapRazorPages();
});

// Redirect to login page for unauthenticated users
app.Use(async (context, next) =>
{
    if (!context.User.Identity.IsAuthenticated && !context.Request.Path.StartsWithSegments("/Login"))
    {
        context.Response.Redirect("/Login"); // Adjust path if necessary
        return;
    }
    await next();
});

app.Run();
