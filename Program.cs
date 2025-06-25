using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using System.Text.Unicode;

var builder = WebApplication.CreateBuilder(args);

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Only add EventLog on Windows
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Logging.AddEventLog();
}

// Add controllers with security (but no global auth for now)
builder.Services.AddControllersWithViews(options =>
{
    // Add global anti-forgery filter
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
    
    // Require HTTPS only if not in development
    if (!builder.Environment.IsDevelopment())
    {
        options.Filters.Add(new RequireHttpsAttribute());
    }
});

// Add Anti-forgery with secure configuration (adjusted for development)
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    if (!builder.Environment.IsDevelopment())
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    }
    else
    {
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    }
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add HTML encoder with enhanced security
builder.Services.AddSingleton<HtmlEncoder>(provider =>
{
    var settings = new TextEncoderSettings();
    settings.AllowRange(UnicodeRanges.BasicLatin);
    settings.AllowRange(UnicodeRanges.Latin1Supplement);
    settings.AllowRange(UnicodeRanges.LatinExtendedA);
    settings.AllowRange(UnicodeRanges.LatinExtendedB);
    return HtmlEncoder.Create(settings);
});

// Register services
builder.Services.AddScoped<CompanySite.Services.DatabaseService>();
builder.Services.AddScoped<CompanySite.Services.ExcelService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Security headers middleware (only in production or when needed)
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        var csp = "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:;";
        context.Response.Headers.Append("Content-Security-Policy", csp);
        
        await next();
    });
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();