using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PIDStandardization.Data.Configuration;
using PIDStandardization.Data.Context;
using PIDStandardization.Data.Repositories;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Services.TaggingServices;
using Serilog;
using System.IO;
using System.Windows;

namespace PIDStandardization.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;

        public App()
        {
            ConfigureLogging();
            ConfigureServices();
        }

        private void ConfigureLogging()
        {
            // Create logs directory if it doesn't exist
            var logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsPath);

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(logsPath, "pidstandardization-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("PID Standardization Application starting up");
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddSerilog(dispose: true);
            });

            // Database configuration
            var dbConfig = new DatabaseConfiguration();
            services.AddDbContext<PIDDbContext>(options =>
                options.UseSqlServer(dbConfig.ConnectionString)
                       .UseLazyLoadingProxies()); // Enable lazy loading for navigation properties

            // Register repositories and Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register services
            services.AddScoped<ITagValidationService, TagValidationService>();

            // Register ViewModels (we'll add these later)
            // services.AddTransient<MainViewModel>();

            // Register Views
            services.AddTransient<MainWindow>();
            services.AddTransient<Views.NewProjectDialog>();

            _serviceProvider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = _serviceProvider?.GetService<MainWindow>();
            mainWindow?.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("PID Standardization Application shutting down");
            Log.CloseAndFlush();
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
