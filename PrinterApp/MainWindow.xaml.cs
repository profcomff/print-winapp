using Serilog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PrinterModel _printerModel = new PrinterModel();
        private readonly Regex _regex = new Regex("[a-zA-Z0-9]{0,6}");

        public MainWindow()
        {
            var fileName = GetType().Namespace;
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File($"logs/{fileName}.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            DataContext = _printerModel.PrinterViewModel;
            InitializeComponent();
            if (40 + Height < SystemParameters.PrimaryScreenHeight)
                Top = 40;
            Left = SystemParameters.PrimaryScreenWidth - 20 - Width;
        }

        private bool IsTextAllowed(string text)
        {
            Log.Debug($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Regex.IsMatch({text}) = {text.Equals(_regex.Match(text).Value)}");
            return text.Equals(_regex.Match(text).Value);
        }

        private void TextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (_printerModel.PrinterViewModel.ErrorTextBlockText != "")
                _printerModel.PrinterViewModel.ErrorTextBlockText = "";
            e.Handled = !IsTextAllowed((sender as TextBox)?.Text + e.Text);
        }

        private void TextBoxPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                if (_printerModel.PrinterViewModel.ErrorTextBlockText != "")
                    _printerModel.PrinterViewModel.ErrorTextBlockText = "";
            }
        }

        private void Print_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                _printerModel.Print(button.Name == "Print2");
            }
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (_printerModel.PrinterViewModel.CodeTextBoxText !=
                "dyakov".ToUpper())
            {
                Log.Information($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Attempt to close without access");
                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }
    }
}