using CalyxManagement.Data;
using DotNetEnv;
using HashKorea_Blazor.Components;
using Microsoft.EntityFrameworkCore;

var solutionDirectory = Directory.GetParent(Directory.GetCurrentDirectory())?.FullName;
var envPath = Path.Combine(solutionDirectory, ".env");

Env.Load(envPath);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapDefaultEndpoints();


// DBContext
builder.Services.AddDbContext<DataContext>(options =>
{
    // 1. MSSQL
    // options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));

    // 2. MYSQL(MariaDB)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

    //var connectionString = string.Format(
    //    builder.Configuration.GetConnectionString("DefaultConnection"),
    //    Environment.GetEnvironmentVariable("AWS_SERVER"),
    //    Environment.GetEnvironmentVariable("AWS_DATABASE"),
    //    Environment.GetEnvironmentVariable("AWS_USER"),
    //    Environment.GetEnvironmentVariable("AWS_PASSWORD")
    //);

    //options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
