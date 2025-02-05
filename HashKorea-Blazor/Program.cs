using HashKorea.Data;
using DotNetEnv;
using HashKorea_Blazor.Components;
using Microsoft.EntityFrameworkCore;
using Radzen;
using Microsoft.AspNetCore.Authentication.Cookies;
using HashKorea.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using HashKorea.DTOs.Auth;
using HashKorea.Common.Constants;

var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
var envPath = Path.Combine(solutionDirectory, ".env");

Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient();

// when you use http request with other api in Service or razor
builder.Services.AddHttpContextAccessor();

// when you use like MVC pattern (controller)
//builder.Services.AddControllersWithViews();

builder.Services.AddScoped<ISharedService, SharedService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ITourMapService, TourMapService>();
builder.Services.AddScoped<IOpenAIService, OpenAIService>();
//builder.Services.AddSingleton<IMemoryManagementService, MemoryManagementService>(); // Redis 처리용 | 중앙에서 하나로 처리하므로 SingleTon으로 처리

// mudblazor
builder.Services.AddMudServices();

// set session
// 1. auth login for kakao -> set session for auth
//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

// radzen blazor
//builder.Services.AddRadzenComponents();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddSingleton(builder.Configuration);

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
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });

// get image of base64 or byte file( you can`t get base64 string from js if you don`t have this one) - start
// nuget: MessagePack
builder.Services.AddSignalR()
    .AddMessagePackProtocol(); // MessagePack 

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 1024 * 1024 * 50; // MAX 50MB
    });

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024 * 1024 * 50; // 50MB
});

// get image of base64 or byte file( you can`t get base64 string from js if you don`t have this one) - end


builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddGoogle(GoogleDefaults.AuthenticationScheme, googleOptions =>
{
    googleOptions.ClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
    googleOptions.ClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
    googleOptions.CallbackPath = new PathString("/signin-google");
})
.AddKakaoTalk("Kakao", kakaoOptions =>
{
    kakaoOptions.ClientId = Environment.GetEnvironmentVariable("KAKAO_CLIENT_ID"); 
    kakaoOptions.ClientSecret = Environment.GetEnvironmentVariable("KAKAO_CLIENT_SECRET");
    kakaoOptions.CallbackPath = new PathString("/signin-kakao");
    kakaoOptions.AuthorizationEndpoint = "https://kauth.kakao.com/oauth/authorize";
    kakaoOptions.TokenEndpoint = "https://kauth.kakao.com/oauth/token";
    kakaoOptions.UserInformationEndpoint = "https://kapi.kakao.com/v2/user/me";

    kakaoOptions.SaveTokens = true;
});


var app = builder.Build();

//app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// TO DO: when you use https
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/signin/kakao", async context =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/signin/kakao/callback",
    };

    await context.ChallengeAsync("Kakao", properties);
});

app.MapGet("/signin/google", async context =>
{
    var properties = new AuthenticationProperties
    {
        RedirectUri = "/signin/google/callback",
    };

    await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, properties);
});

app.MapGet("/signin/kakao/callback", async context =>
{
    var result = await context.AuthenticateAsync("Kakao");

    if (result?.Succeeded != true)
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

app.MapGet("/signin/google/callback", async context =>
{
    var result = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

    if (result?.Succeeded != true)
    {
        context.Response.Redirect("/");
        return;
    }

    var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;
    var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

    if (string.IsNullOrEmpty(googleId))
    {
        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Redirect("/");
        return;
    }

    using var scope = app.Services.CreateScope();
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

    var model = new IsCompletedRequestDto
    {
        Id = googleId,
        SignInType = USER_AUTH.GOOGLE,
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

        var claimsIdentity = new ClaimsIdentity(claims, USER_AUTH.GOOGLE);
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
    context.Response.Redirect("/");
});

DatabaseManagementService.MigrationInitialisation(app);

app.MapDefaultEndpoints();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// when you use controller (for access to api url )
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}")
//    .WithStaticAssets();

// REDIS TEST
//app.Lifetime.ApplicationStarted.Register(async () =>
//{
//    using (var scope = app.Services.CreateScope())
//    {
//        var redisConnection = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
//        var testKey = "testConnection";
//        var testValue = "Redis is working!";

//        await redisConnection.SetStringAsync(testKey, testValue);

//        var valueFromRedis = await redisConnection.GetStringAsync(testKey);
//        if (valueFromRedis != null)
//        {
//            Console.WriteLine($"value in redis: {valueFromRedis}");
//        }
//        else
//        {
//            Console.WriteLine("Redis에서 값을 가져올 수 없습니다.");
//        }
//    }
//});


app.Run();