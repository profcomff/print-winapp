using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Windows;

namespace PrinterApp
{
    public class PrinterModel
    {
        private const string FileUrl = "https://printer.api.profcomff.com/file";
        private const string StaticUrl = "https://printer.api.profcomff.com/static";
        private const string CodeError = "Некорректный код";
        private const string HttpError = "Ошибка сети";

        private const string SumatraError =
            "[Error] program SumatraPdf is not found\ninform the responsible person\n\n[Ошибка] программа SumatraPdf не найдена\nсообщите ответственному лицу";

        private static readonly string SavePath =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
            Path.DirectorySeparatorChar +
            ".printerApp";

        private static readonly string SumatraPathSuffix =
            Path.DirectorySeparatorChar + "SumatraPDF" +
            Path.DirectorySeparatorChar + "SumatraPDF.exe";

        public PrinterViewModel PrinterViewModel { get; } = new PrinterViewModel();

        private Process _currentProcess;

        public PrinterModel()
        {
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);

            if (SearchSumatraPdf() == "")
            {
                MessageBox.Show(SumatraError);
                throw new Exception();
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

        public async void Print(bool printDialog)
        {
            if (PrinterViewModel.CodeTextBoxText.Length < 1)
            {
                PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                PrinterViewModel.ErrorTextBlockText = CodeError;
                return;
            }

            if (PrinterViewModel.CodeTextBoxText.ToUpper() == "IDDQD")
            {
                using (var wc = new WebClient())
                {
                    var saveFilePath = SavePath + Path.DirectorySeparatorChar + "iddqd.pdf";
                    saveFilePath =
                        saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
                    wc.DownloadFileCompleted +=
                        (o, args) => { Print(saveFilePath, new PrintOptions("", 1, false)); };
                    wc.DownloadFileAsync(
                        new Uri("https://cdn.profcomff.com/app/printer/iddqd.pdf"),
                        saveFilePath);
                }

                PrinterViewModel.CodeTextBoxText = "";
                Log.Information($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Easter");
                return;
            }

            Log.Debug($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start response code {PrinterViewModel.CodeTextBoxText}");
            PrinterViewModel.DownloadNotInProgress = false;
            var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync($"{FileUrl}/{PrinterViewModel.CodeTextBoxText}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    try
                    {
                        PrinterViewModel.CodeTextBoxText = "";
                        var responseBody = await response.Content.ReadAsStringAsync();
                        //ReceiveOutput
                        var receiveOutput =
                            JsonConvert.DeserializeObject<ReceiveOutput>(responseBody);
                        if (receiveOutput?.Filename.Length > 0)
                        {
                            DeleteOldFiles();
                            Download(receiveOutput, printDialog);
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.Error($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {exception}");
                        PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                        PrinterViewModel.ErrorTextBlockText = HttpError;
                    }
                }
                else if (response.StatusCode == HttpStatusCode.NotFound ||
                         response.StatusCode == HttpStatusCode.UnsupportedMediaType)
                {
                    PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                    PrinterViewModel.ErrorTextBlockText = CodeError;
                }
            }
            catch (Exception exception)
            {
                Log.Error($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {exception}");
                PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                PrinterViewModel.ErrorTextBlockText = HttpError;
            }

            PrinterViewModel.DownloadNotInProgress = true;

            Log.Debug($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: End response code {PrinterViewModel.CodeTextBoxText}");
        }

        private void Download(ReceiveOutput receiveOutput, bool printDialog = false)
        {
            Log.Debug($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start download filename:{receiveOutput.Filename}");
            using (var wc = new WebClient())
            {
                var name = Guid.NewGuid() + ".pdf";
                var saveFilePath = SavePath + Path.DirectorySeparatorChar + name;
                saveFilePath = saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
                PrinterViewModel.ProgressBarVisibility = Visibility.Visible;
                wc.DownloadProgressChanged += DownloadProgressChanged;
                wc.DownloadFileCompleted +=
                    (sender, args) =>
                    {
                        PrinterViewModel.ProgressBarVisibility = Visibility.Collapsed;
                        Print(saveFilePath, receiveOutput.Options, printDialog);
                    };
                wc.DownloadFileAsync(new Uri($"{StaticUrl}/{receiveOutput.Filename}"),
                    saveFilePath);
            }
            Log.Debug($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: End download filename:{receiveOutput.Filename}");
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            PrinterViewModel.ProgressBarValue = e.ProgressPercentage;
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
                Log.Warning($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {SumatraError}");
                MessageBox.Show(SumatraError);
                throw new Exception();
            }
        }
        private void DeleteOldFiles()
        {
            foreach (FileInfo file in new DirectoryInfo(SavePath).GetFiles())
            {
                file.Delete();
            }
            Log.Debug($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: delete all files complete");
        }
    }
}
