using Serilog;
using System.Reflection;
using System.Windows;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            var fileName = GetType().Namespace;
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File($"logs/{fileName}.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            //Since we no longer have the StarupUri, we have to manually open our window.
            MainWindow = new MainWindow();
            var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            MainWindow.Title = $"{MainWindow.Title} {assemblyVersion}";
            MainWindow.Show();
        }
    }
}
