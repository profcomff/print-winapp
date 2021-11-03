using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json.Linq;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            if (40 + Height < SystemParameters.PrimaryScreenHeight)
                Top = 40;
            Left = SystemParameters.PrimaryScreenWidth - 20 - Width;
        }

        private const string FileUrl = "https://app.profcomff.com/print/file";
        private const string StaticUrl = "https://app.profcomff.com/print/static";
        private const string CodeError = "Не корректный код";
        private const string HttpError = "Ошибка сети";

        private static readonly string SavePath =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Path.DirectorySeparatorChar +
            ".printerApp";

        private static readonly Regex Regex = new Regex("[a-zA-Z0-9]{0,6}");

        private Process _currentProcess;

        private static bool IsTextAllowed(string text)
        {
            Debug.WriteLine("Regex.IsMatch({0}) = {1}", text, text.Equals(Regex.Match(text).Value));
            return text.Equals(Regex.Match(text).Value);
        }

        private void TextBox_OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (ErrorBlock.Text != "")
                ErrorBlock.Text = "";
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
                if (ErrorBlock.Text != "")
                    ErrorBlock.Text = "";
            }
        }

        private async void Print_OnClick(object sender, RoutedEventArgs e)
        {
            if (Code.Text.Length < 1)
            {
                ErrorBlock.Text = CodeError;
                return;
            }

            Debug.WriteLine("start");
            if (sender is Button button)
            {
                Code.IsEnabled = button.IsEnabled = false;
                var httpClient = new HttpClient();
                try
                {
                    var response = await httpClient.GetAsync($"{FileUrl}/{Code.Text}");
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var json = JObject.Parse(responseBody);
                        var fileName = json["filename"]?.ToString();
                        Debug.WriteLine(json["filename"]);
                        if (fileName?.Length > 0)
                        {
                            DeleteOldFiles();
                            Debug.WriteLine("start download");
                            Download(fileName);
                            Debug.WriteLine("end download");
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        ErrorBlock.Text = CodeError;
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                    ErrorBlock.Text = HttpError;
                }

                Code.IsEnabled = button.IsEnabled = true;
            }

            Debug.WriteLine("end");
        }

        private void Download(string filename)
        {
            using (var wc = new WebClient())
            {
                var name = Guid.NewGuid() + ".pdf";
                var saveFilePath = SavePath + Path.DirectorySeparatorChar + name;
                saveFilePath = saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
                ProgressBar.Visibility = Visibility.Visible;
                wc.DownloadProgressChanged += DownloadProgressChanged;
                wc.DownloadFileCompleted +=
                    (sender, args) =>
                    {
                        ProgressBar.Visibility = Visibility.Collapsed;
                        Print(saveFilePath);
                    };
                wc.DownloadFileAsync(new Uri($"{StaticUrl}/{filename}"),
                    saveFilePath);
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void Print(string saveFilePath)
        {
            _currentProcess = new Process();
            _currentProcess.StartInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                Verb = "print",
                FileName = saveFilePath
            };
            _currentProcess.Start();
        }

        private static void DeleteOldFiles()
        {
            foreach (FileInfo file in new DirectoryInfo(SavePath).GetFiles())
            {
                file.Delete();
            }
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (Code.Text != "dyakov")
            {
                Process.Start(Application.ResourceAssembly.Location);
                Application.Current.Shutdown();
            }
        }
    }
}