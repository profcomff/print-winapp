using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;

namespace PrinterApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            InitializeComponent();
            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            if (40 + Height < SystemParameters.PrimaryScreenHeight)
                Top = 40;
            Left = SystemParameters.PrimaryScreenWidth - 20 - Width;

            if (SearchSumatraPdf() == "")
            {
                MessageBox.Show(SumatraError);
                throw new Exception();
            }
        }

        private const string FileUrl = "https://app.profcomff.com/print/file";
        private const string StaticUrl = "https://app.profcomff.com/print/static";
        private const string CodeError = "Некорректный код";
        private const string HttpError = "Ошибка сети";

        private const string SumatraError =
            "[Error] program SumatraPdf is not found\ninform the responsible person\n\n[Ошибка] программа SumatraPdf не найдена\nсообщите ответственному лицу";

        private static readonly string SavePath =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
            Path.DirectorySeparatorChar +
            ".printerApp";

        private static readonly Regex Regex = new Regex("[a-zA-Z0-9]{0,6}");

        private static readonly string SumatraPathSuffix =
            Path.DirectorySeparatorChar + "SumatraPDF" +
            Path.DirectorySeparatorChar + "SumatraPDF.exe";

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

            if (Code.Text.ToUpper() == "IDDQD")
            {
                using (var wc = new WebClient())
                {
                    var saveFilePath = SavePath + Path.DirectorySeparatorChar + "iddqd.pdf";
                    saveFilePath =
                        saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
                    wc.DownloadFileCompleted +=
                        (o, args) => { Print(saveFilePath, new PrintOptions("", 1, false)); };
                    wc.DownloadFileAsync(
                        new Uri("https://dyakov.space/wp-content/uploads/iddqd.pdf"),
                        saveFilePath);
                }

                Code.Text = "";
                return;
            }

            Debug.WriteLine("start");
            if (sender is Button button)
            {
                Code.IsEnabled = Print1.IsEnabled = Print2.IsEnabled = false;
                var httpClient = new HttpClient();
                try
                {
                    var response = await httpClient.GetAsync($"{FileUrl}/{Code.Text}");
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        try
                        {
                            Code.Text = "";
                            var responseBody = await response.Content.ReadAsStringAsync();
                            //ReceiveOutput
                            var receiveOutput =
                                JsonConvert.DeserializeObject<ReceiveOutput>(responseBody);
                            if (receiveOutput?.Filename.Length > 0)
                            {
                                DeleteOldFiles();
                                Debug.WriteLine("start download");
                                Download(receiveOutput, button.Name == "Print2");
                                Debug.WriteLine("end download");
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception);
                            Console.WriteLine(@"2 {0}", exception);
                            ErrorBlock.Text = HttpError;
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.NotFound ||
                             response.StatusCode == HttpStatusCode.UnsupportedMediaType)
                    {
                        ErrorBlock.Text = CodeError;
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);
                    Console.WriteLine(@"1 {0}", exception);
                    ErrorBlock.Text = HttpError;
                }

                Code.IsEnabled = Print1.IsEnabled = Print2.IsEnabled = true;
            }

            Debug.WriteLine("end");
        }

        private void Download(ReceiveOutput receiveOutput, bool printDialog = false)
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
                        Print(saveFilePath, receiveOutput.Options, printDialog);
                    };
                wc.DownloadFileAsync(new Uri($"{StaticUrl}/{receiveOutput.Filename}"),
                    saveFilePath);
            }
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
        }

        private void Print(string saveFilePath, PrintOptions options, bool printDialog = false)
        {
            var sumatraPath = SearchSumatraPdf();
            if (sumatraPath != "")
            {
                _currentProcess = new Process();
                var arguments = "-print-dialog";
                if (!printDialog)
                {
                    arguments =
                        "-print-to-default -print-settings ";
                    arguments += "\"" + (options.TwoSided ? "duplexlong" : "simplex");
                    if (options.Pages != "")
                    {
                        arguments += $",{options.Pages}";
                    }

                    if (options.Copies > 1)
                    {
                        arguments += $",{options.Copies}x";
                    }

                    arguments += "\"";
                }

                var startInfo = new ProcessStartInfo(sumatraPath)
                {
                    Arguments = $"{arguments} {saveFilePath}"
                };
                _currentProcess.StartInfo = startInfo;
                var _ = _currentProcess.Start();
            }
            else
            {
                MessageBox.Show(SumatraError);
                throw new Exception();
            }
        }

        private static void DeleteOldFiles()
        {
            foreach (FileInfo file in new DirectoryInfo(SavePath).GetFiles())
            {
                file.Delete();
            }
        }

        private static string SearchSumatraPdf()
        {
            var path =
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                SumatraPathSuffix;
            if (File.Exists(path))
                return path;
            path = Directory.GetCurrentDirectory() + SumatraPathSuffix;
            if (File.Exists(path))
                return path;
            path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "SumatraPDF.exe";
            if (File.Exists(path))
                return path;
            path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) +
                   SumatraPathSuffix;
            if (File.Exists(path))
                return path;
            path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) +
                   SumatraPathSuffix;
            return File.Exists(path) ? path : "";
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