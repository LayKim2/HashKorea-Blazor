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

var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
var envPath = Path.Combine(solutionDirectory, ".env");

Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// when you use http request with other api in Service
builder.Services.AddHttpContextAccessor();

// when you use like MVC pattern (controller)
builder.Services.AddControllersWithViews();

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


//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
//})
//.AddCookie()
//.AddKakaoTalk(options =>
//{
//    options.ClientId = Environment.GetEnvironmentVariable("KAKAO_CLIENT_ID");
//    options.ClientSecret = Environment.GetEnvironmentVariable("KAKAO_CLIENT_SECRET");

//    var redirectUri = Environment.GetEnvironmentVariable("KAKAO_REDIRECT_URI");
//    var uri = new Uri(redirectUri);
//    options.CallbackPath = new PathString(uri.AbsolutePath);
//});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddAuthentication(AppConstants.AuthScheme)
    .AddCookie(AppConstants.AuthScheme, cookieOptions =>
    {
        cookieOptions.Cookie.Name = AppConstants.AuthScheme;
    })
    .AddGoogle(GoogleDefaults.AuthenticationScheme, googleOptions =>
    {
        googleOptions.ClientId = "94605561544-md5tee3juotjinm66p7qtgo8ubqts5jj.apps.googleusercontent.com";
        googleOptions.ClientSecret = "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";
        googleOptions.AccessDeniedPath = "/access-denied";
        googleOptions.SignInScheme = AppConstants.AuthScheme;
        googleOptions.CallbackPath = new PathString("/signin-google");  // 리디렉션 경로
    })
    .AddKakaoTalk("Kakao", kakaoOptions =>
    {
        kakaoOptions.ClientId = "b4f436e21e363b5faef09df87c109967";  // 카카오 REST API 키
        kakaoOptions.ClientSecret = "3T3tFIBHzC9MsvhbpKPXcCmuTkJum55m";  // 선택 사항
        kakaoOptions.CallbackPath = new PathString("/signin-kakao");  // 리디렉션 경로
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

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/login"))
    {
        var authProperties = new AuthenticationProperties
        {
            RedirectUri = "/after-login"
        };

        //await context.ChallengeAsync(GoogleDefaults.AuthenticationScheme, authProperties);
        await context.ChallengeAsync("Kakao", authProperties);


        return;
    }

    await next();
});

DatabaseManagementService.MigrationInitialisation(app);

app.MapDefaultEndpoints();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// when you use controller (for access to api url )
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

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