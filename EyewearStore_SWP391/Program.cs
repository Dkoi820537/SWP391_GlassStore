using EyewearStore_SWP391.Models;
using EyewearStore_SWP391.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

// Razor + DbContext
builder.Services.AddRazorPages();
builder.Services.AddDbContext<EyewearStoreContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnectionString")));

// Add API Controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Eyewear Store API",
        Description = "A .NET 8 Web API for managing eyewear products including lenses, frames, and accessories",
        Contact = new OpenApiContact
        {
            Name = "Eyewear Store Support",
            Email = "support@eyewearstore.com"
        }
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register application services
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ILensService, LensService>();
builder.Services.AddScoped<IFrameService, FrameService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IStripeService, StripeService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddSingleton<IOtpService, OtpService>();
// Configure Stripe
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Authentication (cookie) - secure defaults and RememberMe handling
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Paths
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // Cookie settings
        options.Cookie.Name = "LensadeAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // change to Always when deploying with HTTPS
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Optional: event to avoid redirect for API calls
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api") && !ctx.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Eyewear Store API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Map controllers & razor pages
app.MapControllers();

app.MapRazorPages();

app.Run();