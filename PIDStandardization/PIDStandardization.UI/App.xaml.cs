using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PIDStandardization.Data.Configuration;
using PIDStandardization.Data.Context;
using PIDStandardization.Data.Repositories;
using PIDStandardization.Core.Interfaces;
using PIDStandardization.Services.TaggingServices;
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
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

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
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}
