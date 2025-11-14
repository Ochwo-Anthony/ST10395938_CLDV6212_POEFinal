// Program.cs
using System.Globalization;
using ABCRetailers.Data;
using ABCRetailers.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Session Configuration
builder.Services.AddDistributedMemoryCache(); // Required for session storage
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session expires after 30 minutes of inactivity
    options.Cookie.HttpOnly = true; // Prevent JavaScript access for security
    options.Cookie.IsEssential = true; // GDPR compliance - cannot be declined
    options.Cookie.Name = "ABCRetailers.Session";
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.None  // Allow HTTP in development
        : CookieSecurePolicy.Always; // Require HTTPS in production
});

// Typed HttpClient for your Azure Functions
builder.Services.AddHttpClient("Functions", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var baseUrl = cfg["Functions:BaseUrl"] ?? throw new InvalidOperationException("Functions:BaseUrl missing");
    client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/api/"); // adjust if your Functions don't use /api
    client.Timeout = TimeSpan.FromSeconds(100);
});

// Use the typed client (replaces IAzureStorageService everywhere)
builder.Services.AddScoped<IFunctionsApi, FunctionsApiClient>();

// Optional: allow larger multipart uploads (images, proofs, etc.)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
});

// Optional: logging is added by default, keeping this is harmless
builder.Services.AddLogging();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnection")));

var app = builder.Build();

// Culture (your original fix for decimal handling)
var culture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// Add Session middleware - IMPORTANT: Must be after UseRouting and before MapControllerRoute
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}");

app.Run();