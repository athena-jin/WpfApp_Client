using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace WpfApp_Client
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			DataContext = App.ServiceProvider.GetRequiredService<MainWindowVM>();
		}
	}
}