using HashKorea.Data;
using DotNetEnv;
using HashKorea_Blazor.Components;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using HashKorea.Services;
using HashKorea.DTOs.Auth;
using System.Security.Claims;
using HashKorea.Common.Constants;

var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
var envPath = Path.Combine(solutionDirectory, ".env");

Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// when you use http request with other api in Service
builder.Services.AddHttpContextAccessor();

// when you use like MVC pattern (controller)
//builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ISharedService, SharedService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// mudblazor
builder.Services.AddMudServices();

// set session
// 1. auth login for kakao -> set session for auth
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// radzen blazor
//builder.Services.AddRadzenComponents();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// DBContext
builder.Services.AddDbContext<DataContext>(options =>
{
    // 1. MSSQL
    // options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // 2. MYSQL(MariaDB)
    //var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    //options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

    var connectionString = string.Format(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        Environment.GetEnvironmentVariable("AWS_SERVER"),
        Environment.GetEnvironmentVariable("AWS_DATABASE"),
        Environment.GetEnvironmentVariable("AWS_USER"),
        Environment.GetEnvironmentVariable("AWS_PASSWORD")
    );

    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddKakaoTalk(options =>
{
    options.ClientId = Environment.GetEnvironmentVariable("KAKAO_CLIENT_ID");
    options.ClientSecret = Environment.GetEnvironmentVariable("KAKAO_CLIENT_SECRET");

    var redirectUri = Environment.GetEnvironmentVariable("KAKAO_REDIRECT_URI");
    var uri = new Uri(redirectUri);
    options.CallbackPath = new PathString(uri.AbsolutePath);
});


var app = builder.Build();

app.UseSession();

DatabaseManagementService.MigrationInitialisation(app);

app.MapDefaultEndpoints();



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/signin-kakao", async context =>
{
    var state = Guid.NewGuid().ToString();
    context.Session.SetString("AuthState", state);

    var properties = new AuthenticationProperties
    {
        RedirectUri = "/signin-kakao-callback",
        Items =
        {
            { "state", state }
        }
    };

    await context.ChallengeAsync("KakaoTalk", properties);
});

app.MapGet("/signin-kakao-callback", async context =>
{
    var result = await context.AuthenticateAsync("KakaoTalk");

    if (result?.Succeeded != true ||
        result.Properties.Items["state"] != context.Session.GetString("AuthState"))
    {
        context.Response.Redirect("/");
        return;
    }

    var kakaoId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
    var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

    if (string.IsNullOrEmpty(kakaoId))
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Redirect("/");
        return;
    }

    using var scope = app.Services.CreateScope();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

    var model = new IsCompletedRequestDto
    {
        Id = kakaoId,
        SignInType = USER_AUTH.KAKAO,
        Name = name ?? string.Empty,
        Email = email ?? string.Empty
    };

    var isCompletedResponse = await authService.IsCompleted(model);
    if (isCompletedResponse.Success && isCompletedResponse.Data != null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, isCompletedResponse.Data.id),
            new Claim(ClaimTypes.Name, isCompletedResponse.Data.name)
        };

        var claimsIdentity = new ClaimsIdentity(claims, USER_AUTH.KAKAO);
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);
    }
    else
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    context.Response.Redirect("/");
});

app.MapGet("/signout", async context =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Session.Clear();
    context.Response.Redirect("/");
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// when you use controller (for access to api url )
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}")
//    .WithStaticAssets();

app.Run();
