using Newtonsoft.Json;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace PrinterApp
{
    public class PrinterModel
    {
#if DEBUG
        private const string FileUrl = "https://printer.api.test.profcomff.com/file";
        private const string StaticUrl = "https://printer.api.test.profcomff.com/static";
        private const string WebSockUrl = "wss://printer.api.test.profcomff.com/qr";
#else
        private const string FileUrl = "https://printer.api.profcomff.com/file";
        private const string StaticUrl = "https://printer.api.profcomff.com/static";
        private const string WebSockUrl = "wss://printer.api.profcomff.com/qr";
#endif
        private const string CodeError = "Некорректный код";
        private const string HttpError = "Ошибка сети";

        private const string SumatraError =
            "[Error] program SumatraPdf is not found\ninform the responsible person\n\n[Ошибка] программа SumatraPdf не найдена\nсообщите ответственному лицу";

        private static readonly string SumatraPathSuffix =
            Path.DirectorySeparatorChar + "SumatraPDF" +
            Path.DirectorySeparatorChar + "SumatraPDF.exe";

        public PrinterViewModel PrinterViewModel { get; } = new();

        private readonly ConfigFile _configFile;
        private readonly AutoUpdater _autoUpdater;
        private readonly HttpClient _httpClient;
        private bool _socketClose;

        public PrinterModel(ConfigFile configFile, AutoUpdater autoUpdater)
        {
            _configFile = configFile;
            _autoUpdater = autoUpdater;

            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("token", _configFile.AuthorizationToken);

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

            SocketsStartAsync();

            Marketing.LoadProgram();
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

        public async void PrintAsync(bool printDialog)
        {
            if (PrinterViewModel.CodeTextBoxText.Length < 1)
            {
                PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                PrinterViewModel.ErrorTextBlockText = CodeError;
                return;
            }

            PrinterViewModel.DownloadNotInProgress = false;
            if (PrinterViewModel.CodeTextBoxText.ToUpper() == "IDDQD")
            {
                var saveFilePath = _configFile.TempSavePath + Path.DirectorySeparatorChar +
                                   "iddqd.pdf";
                saveFilePath =
                    saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
                PrinterViewModel.PrintQrVisibility = Visibility.Collapsed;
                ShowComplement();
                await using var s =
                    await _httpClient.GetStreamAsync(
                        "https://cdn.profcomff.com/app/printer/iddqd.pdf");
                await using var fs = new FileStream(saveFilePath, FileMode.OpenOrCreate);
                await s.CopyToAsync(fs);
                PrintFile(saveFilePath, new PrintOptions("", 1, false),
                    patchFrom: "");
                PrinterViewModel.CodeTextBoxText = "";
                PrinterViewModel.DownloadNotInProgress = true;
                Log.Information(
                    $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Easter");

                return;
            }

            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start response code {PrinterViewModel.CodeTextBoxText}");
            var patchFrom = $"{FileUrl}/{PrinterViewModel.CodeTextBoxText}";
            PrinterViewModel.DownloadNotInProgress = false;
            try
            {
                var response =
                    await _httpClient.GetAsync($"{FileUrl}/{PrinterViewModel.CodeTextBoxText}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Marketing.CheckCode(
                        statusOk: true,
                        pathFrom: patchFrom);

                    try
                    {
                        PrinterViewModel.CodeTextBoxText = "";
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var fileWithOptions =
                            JsonConvert.DeserializeObject<FileWithOptions>(responseBody);

                        if (fileWithOptions != null && fileWithOptions.Filename.Length > 0)
                        {
                            DeleteOldFiles();
                            Marketing.StartDownload(
                                pathFrom: patchFrom,
                                pathTo: $"{StaticUrl}/{fileWithOptions.Filename}");
                            await Download(fileWithOptions, patchFrom, printDialog);
                        }
                        else
                        {
                            Marketing.PrintNotFile(pathFrom: patchFrom);
                        }
                    }
                    catch (Exception exception)
                    {
                        Marketing.DownloadException(status: exception.Message,
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
                    Marketing.CheckCode(statusOk: false, pathFrom: patchFrom);

                    PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                    PrinterViewModel.ErrorTextBlockText = CodeError;
                }
            }
            catch (Exception exception)
            {
                Marketing.PrintException(status: exception.Message, pathFrom: patchFrom);

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

        private async Task Download(FileWithOptions fileWithOptions, string patchFrom,
            bool printDialog = false)

        {
            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start download filename:{fileWithOptions.Filename}");
            var name = Guid.NewGuid() + ".pdf";
            var saveFilePath = _configFile.TempSavePath + Path.DirectorySeparatorChar + name;
            saveFilePath = saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
            PrinterViewModel.PrintQrVisibility = Visibility.Collapsed;
            ShowComplement();
            await using var s =
                await _httpClient.GetStreamAsync($"{StaticUrl}/{fileWithOptions.Filename}");
            await using var fs = new FileStream(saveFilePath, FileMode.OpenOrCreate);
            await s.CopyToAsync(fs);
            Marketing.FinishDownload(pathFrom: patchFrom,
                pathTo: $"{StaticUrl}/{fileWithOptions.Filename}");
            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: End download filename:{fileWithOptions.Filename}");
            PrintFile(saveFilePath, fileWithOptions.Options, patchFrom, printDialog);
        }

        private void PrintFile(string saveFilePath, PrintOptions options, string patchFrom,
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

                Marketing.StartSumatra(pathFrom: patchFrom);

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

        private void ShowComplement()
        {
            new Task(async () =>
            {
                PrinterViewModel.Compliment = Compliments.GetRandomCompliment();
                await Task.Delay(5000);
                PrinterViewModel.Compliment = "";
                PrinterViewModel.PrintQrVisibility = Visibility.Visible;
            }).Start();
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null! || imageData.Length == 0) return null!;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }

        public void SocketsClose()
        {
            _socketClose = true;
        }

        private async void SocketsStartAsync()
        {
            var socket = new ClientWebSocket();
            socket.Options.SetRequestHeader("Authorization",
                _httpClient.DefaultRequestHeaders.Authorization!.ToString());
            try
            {
                await socket.ConnectAsync(new Uri(WebSockUrl), CancellationToken.None);
                if (socket.State != WebSocketState.Open)
                {
                    Marketing.SocketException(
                        status: $"WebSocketState not Open state:{socket.State}");
                    Log.Error(
                        $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: WebSocketState not Open state:{socket.State}");
                    return;
                }

                _socketClose = false;
                var buffer = new byte[128 * 1024];
                while (!_socketClose)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer),
                        CancellationToken.None);
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (!result.EndOfMessage)
                    {
                        Thread.Sleep(100);
                        result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer),
                            CancellationToken.None);
                        json += Encoding.UTF8.GetString(buffer, 0, result.Count);
                    }

                    await ParseResponseFromSocket(
                        JsonConvert.DeserializeObject<WebsocketReceiveOptions>(json));
                }

                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Good Bye",
                    CancellationToken.None);
            }
            catch (Exception exception)
            {
                _socketClose = true;
                Marketing.SocketException(status: exception.Message);
                Log.Error($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {exception}");
                PrinterViewModel.PrintQr = null!;
                socket.Abort();
                    await Task.Delay(5000);
                    SocketsStartAsync();
            }
        }

        private async Task ParseResponseFromSocket(WebsocketReceiveOptions? websocketReceiveOptions)
        {
            if (websocketReceiveOptions == null)
            {
                Marketing.SocketException("websocketReceiveOptions is null");
                return;
            }

            if (websocketReceiveOptions.ManualUpdate)
            {
                _autoUpdater.ManualUpdate();
            }

            if (websocketReceiveOptions.QrToken == null!)
            {
                Marketing.SocketException("websocketReceiveOptions QrToken is null");
                return;
            }

            PrinterViewModel.PrintQr = null!;
            PrinterViewModel.DownloadNotInProgress = false;
            if (websocketReceiveOptions.Files != null!)
            {
                DeleteOldFiles();
                foreach (var fileWithOptions in websocketReceiveOptions.Files)
                {
                    const string patchFrom = "websocket";
                    try
                    {
                        PrinterViewModel.CodeTextBoxText = "";

                        if (fileWithOptions.Filename.Length > 0)
                        {
                            Marketing.StartDownload(
                                pathFrom: patchFrom,
                                pathTo: $"{StaticUrl}/{fileWithOptions.Filename}");
                            await Download(fileWithOptions, patchFrom);
                        }
                        else
                        {
                            Marketing.PrintNotFile(pathFrom: patchFrom);
                        }
                    }
                    catch (Exception exception)
                    {
                        Marketing.DownloadException(status: exception.Message,
                            pathFrom: patchFrom);

                        Log.Error(
                            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {exception}");
                        PrinterViewModel.ErrorTextBlockVisibility = Visibility.Visible;
                        PrinterViewModel.ErrorTextBlockText = HttpError;
                    }
                }
            }

            PrinterViewModel.DownloadNotInProgress = true;
            GenerateQr(websocketReceiveOptions.QrToken);
        }

        private void GenerateQr(string value)
        {
            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: new qr code {value}");
            var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrCodeData);
            byte[] white = { 255, 255, 255, 255 };
            byte[] transparent = { 0, 0, 0, 0 };
            var qrCodeImage = qrCode.GetGraphic(20, white, transparent, false);
            PrinterViewModel.PrintQr = LoadImage(qrCodeImage);
        }
    }
}