using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace WpfApp_Client
{
	public partial class App : Application
	{
		public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            ServiceProvider = serviceCollection.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            //mainWindow.Closed += (sender, e) => Shutdown();
            mainWindow.Closed += MainWindow_Closed;
            mainWindow.Show();

            base.OnStartup(e);
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ConfigureServices(IServiceCollection services)
		{
			services.AddDbContext<HJ_DbContext>(options =>
				options.UseSqlite("Data Source=db/mydatabase.db"));
			services.AddTransient<MainWindow>();
			services.AddTransient<MainWindowVM>();
		}
	}
}