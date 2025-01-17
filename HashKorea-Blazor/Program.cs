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
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Authentication.OAuth;

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
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddSingleton<IMemoryManagementService, MemoryManagementService>(); // Redis 처리용 | 중앙에서 하나로 처리하므로 SingleTon으로 처리

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

//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
//});

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

// Redis
builder.Services.AddStackExchangeRedisCache(option =>
    //option.Configuration = Environment.GetEnvironmentVariable("Cache"));
    option.Configuration = "hashkorea.cache:6379");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// get image of base64 or byte file( you can`t get base64 string from js if you don`t have this one) - start
// nuget: MessagePack
builder.Services.AddSignalR()
    .AddMessagePackProtocol(); // MessagePack 

builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 1024 * 1024 * 50; // MAX 50MB
    });

// get image of base64 or byte file( you can`t get base64 string from js if you don`t have this one) - end


builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie()
.AddKakaoTalk(options =>
{
    // KakaoTalk OAuth 설정
    options.ClientId = Environment.GetEnvironmentVariable("KAKAO_CLIENT_ID");
    options.ClientSecret = Environment.GetEnvironmentVariable("KAKAO_CLIENT_SECRET");

    var redirectUri = Environment.GetEnvironmentVariable("KAKAO_REDIRECT_URI");
    var uri = new Uri(redirectUri);
    options.CallbackPath = new PathString(uri.AbsolutePath);

    // OAuth 디버깅을 위한 이벤트 설정
    options.Events = new OAuthEvents
    {
        OnRedirectToAuthorizationEndpoint = context =>
        {
            // 리디렉션 전에 state 로그 출력
            if (context.Properties.Items.TryGetValue("state", out var state))
            {
                Console.WriteLine($"[DEBUG] Redirecting to Kakao. Generated state: {state}");
            }
            else
            {
                Console.WriteLine("[DEBUG] Redirecting to Kakao. No state found in context properties.");
            }

            Console.WriteLine($"[DEBUG] context.RedirectUri: {context.RedirectUri}");

            // 리디렉션 실행
            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        },
        OnRemoteFailure = context =>
        {
            // 인증 실패 로그
            Console.WriteLine($"[ERROR] Remote failure occurred: {context.Failure?.Message}");
            return Task.CompletedTask;
        },
        OnCreatingTicket = context =>
        {
            // 콜백에서 state 확인
            if (context.Properties.Items.TryGetValue("state", out var state))
            {
                Console.WriteLine($"[DEBUG] Creating ticket. State from callback: {state}");
            }
            else
            {
                Console.WriteLine("[DEBUG] Creating ticket. No state found in callback.");
            }

            return Task.CompletedTask;
        }
    };
});


var app = builder.Build();

//app.UseSession();

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

app.MapGet("/signin-kakao", async (HttpContext context, IMemoryManagementService memoryService) =>
{
    var state = Guid.NewGuid().ToString("N");

    Console.WriteLine($"start to sign in kakao!! and state:" + state);

    await memoryService.SetAsync($"AuthState:{state}", state, TimeSpan.FromMinutes(5));

    Console.WriteLine($"start to sign in kakao!! 22");

    var retrievedState = await memoryService.GetAsync<string>($"AuthState:{state}");

    if (retrievedState != null)
    {
        Console.WriteLine($"Retrieved value: {retrievedState}");
    }
    else
    {
        Console.WriteLine("Value not found or expired.");
    }

    var properties = new AuthenticationProperties
    {
        RedirectUri = "/signin-kakao-callback",
        //Items =
        //{
        //    { "state", state }
        //}
    };

    properties.Items["state"] = state;

    await context.ChallengeAsync("KakaoTalk", properties);
});

//app.MapGet("/signin-kakao", async context =>
//{
//    var state = Guid.NewGuid().ToString();
//    context.Session.SetString("AuthState", state);

//    var properties = new AuthenticationProperties
//    {
//        RedirectUri = "/signin-kakao-callback",
//        Items =
//        {
//            { "state", state }
//        }
//    };

//    await context.ChallengeAsync("KakaoTalk", properties);
//});

app.MapGet("/signin-kakao-callback", async context =>
{
    // you can see the log in aws ec2.
    Console.WriteLine($"signin-kakao-callback");

    var result = await context.AuthenticateAsync("KakaoTalk");

    if (result?.Succeeded != true)
    {
        context.Response.Redirect("/");
        return;
    }

    Console.WriteLine($"signin-kakao-callback 2");

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
    //context.Session.Clear();
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

app.Lifetime.ApplicationStarted.Register(async () =>
{
    using (var scope = app.Services.CreateScope())
    {
        var redisConnection = scope.ServiceProvider.GetRequiredService<IDistributedCache>();
        var testKey = "testConnection";
        var testValue = "Redis is working!";

        await redisConnection.SetStringAsync(testKey, testValue);

        var valueFromRedis = await redisConnection.GetStringAsync(testKey);
        if (valueFromRedis != null)
        {
            Console.WriteLine($"value in redis: {valueFromRedis}");
        }
        else
        {
            Console.WriteLine("Redis에서 값을 가져올 수 없습니다.");
        }
    }
});


app.Run();