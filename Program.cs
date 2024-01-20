using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.FeatureManagement;
using SmartCarWebApp.Services;
using SmartCarWebApp.Shared;

namespace SmartCarWebApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddAzureWebAppDiagnostics();
        builder.Services.Configure<AzureFileLoggerOptions>(options =>
        {
            options.FileName = "azure-diagnostics-";
            options.FileSizeLimit = 50 * 1024;
            options.RetainedFileCountLimit = 5;
        });

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddHttpClient();
        builder.Services.AddScoped<SmartCarService>();
        builder.Services.AddAzureAppConfiguration();

        // Load configuration from Azure App Configuration
        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(builder.Configuration["ConnectionStrings:AppConfig"])
                   // Load all keys that start with `SmartCarApp:`
                   .Select("SmartCarApp:*")
                   // Configure to reload configuration if the registered sentinel key is modified
                   .ConfigureRefresh(refreshOptions =>
                        refreshOptions.Register("SmartCarApp:Settings:Sentinel", refreshAll: true));

            options.UseFeatureFlags(featureFlagOptions =>
            {
                featureFlagOptions.Select("SmartCarApp:*");
            });
        });

        builder.Services.Configure<Settings>(builder.Configuration.GetSection("SmartCarApp:Settings"));

        builder.Services.AddFeatureManagement(); // must be after azure so local app settings takes precedence

        // temporary until a DB is implemented:
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSession();

        // Override Azure Configuration with Local Settings in Development
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            LoggingExtensions.UseColoring = true;
        }

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        
         app.UseAzureAppConfiguration();

        app.UseRouting();

        app.UseAuthorization();

        // temporary until a DB is implemented:
        app.UseSession(new SessionOptions()
        {
            Cookie = new CookieBuilder()
            {
                Name = ".AspNetCore.Session.SmartCarApp"
            }
        });
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        
        app.Run();
    }
}
