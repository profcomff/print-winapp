using Serilog;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;

namespace PrinterApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly AutoUpdater _autoUpdater = new();
    private readonly ConfigFile _configFile = new();
    private readonly MemoryMonitor _memoryMonitor = new();
    private PrinterModel _printerModel;
    private MainWindow _mainWindow;

    private readonly string _assemblyVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    private App()
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

        Thread.CurrentThread.CurrentCulture =
            System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

        var fileName = GetType().Namespace!;
        ConfigureLogger(fileName);
        _configFile.LoadConfig(fileName);
        if (!Directory.Exists(_configFile.TempSavePath))
            Directory.CreateDirectory(_configFile.TempSavePath);

        var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        var str = Environment.ProcessPath ?? string.Empty;
        key?.SetValue(GetType().Namespace, _configFile.StartWithWindows ? str : string.Empty);

        if (_configFile.AutoUpdate) _autoUpdater.StartTimer();

        _printerModel = new PrinterModel(_configFile, _autoUpdater);
        _printerModel.Reboot += RebootHandler;
        _memoryMonitor.StartTimer();
        _mainWindow = new MainWindow(_printerModel);
        _mainWindow.Title = $"{_mainWindow.Title} {_assemblyVersion}";
        _mainWindow.Closing += MainWindowClosing;
        _mainWindow.Closed += MainWindowClosed;

        Marketing.LoadProgram();
    }

    private void CurrentDomainOnUnhandledException(object sender,
        UnhandledExceptionEventArgs e)
    {
        Log.Error((Exception)e.ExceptionObject, "CurrentDomainOnUnhandledException");
    }

    private void RebootHandler()
    {
        _printerModel.PrinterViewModel.DownloadNotInProgress = false;
        _printerModel.PrinterViewModel.PrintQrVisibility = Visibility.Collapsed;
        _printerModel.PrinterViewModel.CodeTextBoxText = "REBOOT";
        _mainWindow.Close();
    }

    private void MainWindowClosed(object? sender, EventArgs e)
    {
        if (_printerModel.PrinterViewModel.CodeTextBoxText != "REBOOT") return;
        _printerModel = new PrinterModel(_configFile, _autoUpdater);
        _printerModel.Reboot += RebootHandler;
        _mainWindow = new MainWindow(_printerModel);
        _mainWindow.Title = $"{_mainWindow.Title} {_assemblyVersion}";
        _mainWindow.Closing += MainWindowClosing;
        _mainWindow.Closed += MainWindowClosed;
        _mainWindow.Show();
    }

    private void MainWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        switch (_printerModel.PrinterViewModel.CodeTextBoxText)
        {
            case "UPDATE":
                _printerModel.PrinterViewModel.CodeTextBoxText = "";
                _autoUpdater.ManualUpdate();
                e.Cancel = true;
                return;
            case "REBOOT":
                _printerModel.SocketsClose();
                Marketing.ManualReboot();
                return;
        }

        if (_printerModel.WrongExitCode())
        {
            Marketing.CloseWithoutAccessProgram();
            Log.Information(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Attempt to close without access");
            e.Cancel = true;
            return;
        }

        Marketing.ManualShutdown();
        _printerModel.SocketsClose();
        _autoUpdater.StopTimer();
        _memoryMonitor.StopTimer();
        Current.Shutdown();
    }

    private void App_OnStartup(object sender, StartupEventArgs e)
    {
        _mainWindow.Show();
    }

    private void App_OnExit(object sender, ExitEventArgs e)
    {
        Log.Debug("App_OnExit");
        Log.CloseAndFlush();
    }

    private static void ConfigureLogger(string fileName)
    {
#if DEBUG
        var log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug()
            .WriteTo.File(
                $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}.printerAppLogs/{fileName}.txt",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
#else
        var log = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}{Path.DirectorySeparatorChar}.printerAppLogs/{fileName}.txt",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
#endif
        Log.Logger = log;
    }
}