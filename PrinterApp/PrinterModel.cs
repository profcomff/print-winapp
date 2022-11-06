using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
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

        private static readonly string SumatraPathSuffix =
            Path.DirectorySeparatorChar + "SumatraPDF" +
            Path.DirectorySeparatorChar + "SumatraPDF.exe";

        public PrinterViewModel PrinterViewModel { get; } = new();

        private readonly ConfigFile _configFile;

        public PrinterModel(ConfigFile configFile)
        {
            _configFile = configFile;

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (!Directory.Exists(_configFile.TempSavePath))
                Directory.CreateDirectory(_configFile.TempSavePath);

            var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            var str = Environment.ProcessPath ?? string.Empty;
            key?.SetValue(GetType().Namespace, _configFile.StartWithWindows ? str : string.Empty);

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
                    var saveFilePath = _configFile.TempSavePath + Path.DirectorySeparatorChar +
                                       "iddqd.pdf";
                    saveFilePath =
                        saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
                    wc.DownloadFileCompleted += async (o, args) =>
                    {
                        await Print(saveFilePath, new PrintOptions("", 1, false),
                            patchFrom: "");
                    };
                    wc.DownloadFileAsync(
                        new Uri("https://cdn.profcomff.com/app/printer/iddqd.pdf"),
                        saveFilePath);
                }

                PrinterViewModel.CodeTextBoxText = "";
                Log.Information($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Easter");
                return;
            }

            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start response code {PrinterViewModel.CodeTextBoxText}");
            var patchFrom = $"{FileUrl}/{PrinterViewModel.CodeTextBoxText}";
            PrinterViewModel.DownloadNotInProgress = false;
            var httpClient = new HttpClient();
            try
            {
                var response =
                    await httpClient.GetAsync($"{FileUrl}/{PrinterViewModel.CodeTextBoxText}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    await Marketing.CheckCode(
                        statusOk: true,
                        pathFrom: patchFrom);

                    try
                    {
                        PrinterViewModel.CodeTextBoxText = "";
                        var responseBody = await response.Content.ReadAsStringAsync();
                        //ReceiveOutput
                        var receiveOutput =
                            JsonConvert.DeserializeObject<ReceiveOutput>(responseBody);

                        if (receiveOutput != null && receiveOutput.Filename.Length > 0)
                        {
                            DeleteOldFiles();
                            await Marketing.StartDownload(
                                pathFrom: patchFrom,
                                pathTo: $"{StaticUrl}/{receiveOutput.Filename}");
                            Download(receiveOutput, patchFrom, printDialog);
                        }
                        else
                        {
                            await Marketing.PrintNotFile(pathFrom: patchFrom);
                        }
                    }
                    catch (Exception exception)
                    {
                        await Marketing.DownloadException(status: exception.Message,
                            pathFrom: patchFrom);

                        Log.Error(
                            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {exception}");
                        PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                        PrinterViewModel.ErrorTextBlockText = HttpError;
                    }
                }
                else if (response.StatusCode is HttpStatusCode.NotFound
                         or HttpStatusCode.UnsupportedMediaType)
                {
                    await Marketing.CheckCode(statusOk: false, pathFrom: patchFrom);

                    PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                    PrinterViewModel.ErrorTextBlockText = CodeError;
                }
            }
            catch (Exception exception)
            {
                await Marketing.PrintException(status: exception.Message, pathFrom: patchFrom);

                Log.Error($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {exception}");
                PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                PrinterViewModel.ErrorTextBlockText = HttpError;
            }

            PrinterViewModel.DownloadNotInProgress = true;

            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: End response code {PrinterViewModel.CodeTextBoxText}");
        }

        public bool WrongExitCode()
        {
            return PrinterViewModel.CodeTextBoxText != _configFile.ExitCode.ToUpper();
        }

        private void Download(ReceiveOutput receiveOutput, string patchFrom, bool printDialog =
            false)

        {
            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start download filename:{receiveOutput.Filename}");
            using (var wc = new WebClient())
            {
                var name = Guid.NewGuid() + ".pdf";
                var saveFilePath = _configFile.TempSavePath + Path.DirectorySeparatorChar + name;
                saveFilePath = saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
                PrinterViewModel.ProgressBarVisibility = Visibility.Visible;
                wc.DownloadProgressChanged += DownloadProgressChanged;
                wc.DownloadFileCompleted += async (sender, args) =>
                {
                    await Marketing.FinishDownload(pathFrom: patchFrom,
                        pathTo: $"{StaticUrl}/{receiveOutput.Filename}");
                    PrinterViewModel.ProgressBarVisibility = Visibility.Collapsed;
                    await Print(saveFilePath, receiveOutput.Options, patchFrom, printDialog);
                };
                wc.DownloadFileAsync(new Uri($"{StaticUrl}/{receiveOutput.Filename}"),
                    saveFilePath);
            }

            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: End download filename:{receiveOutput.Filename}");
        }

        private void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            PrinterViewModel.ProgressBarValue = e.ProgressPercentage;
        }

        private async Task Print(string saveFilePath, PrintOptions options, string patchFrom,
            bool printDialog = false)
        {
            var sumatraPath = SearchSumatraPdf();
            if (sumatraPath != "")
            {
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

                await Marketing.StartSumatra(pathFrom: patchFrom);

                Process currentProcess = new() { StartInfo = startInfo };
                var _ = currentProcess.Start();
            }
            else
            {
                Log.Warning(
                    $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {SumatraError}");
                MessageBox.Show(SumatraError);
                throw new Exception();
            }
        }

        private void DeleteOldFiles()
        {
            foreach (FileInfo file in new DirectoryInfo(_configFile.TempSavePath).GetFiles())
            {
                file.Delete();
            }

            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: delete all files complete");
        }
    }
}