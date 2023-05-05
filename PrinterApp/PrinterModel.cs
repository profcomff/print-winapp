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
using QRCoder;
using QRCoder.Xaml;

namespace PrinterApp;

public class PrinterModel
{
#if DEBUG
    private const string FileUrl = "https://api.test.profcomff.com/print/file";
    private const string StaticUrl = "https://api.test.profcomff.com/print/static";
    private const string WebSockUrl = "wss://api.test.profcomff.com/print/qr";
#else
        private const string FileUrl = "https://api.profcomff.com/print/file";
        private const string StaticUrl = "https://api.profcomff.com/print/static";
        private const string WebSockUrl = "wss://api.profcomff.com/print/qr";
#endif
    private const string CodeError = "Некорректный код";
    private const string HttpError = "Ошибка сети";

    private const string SumatraError =
        "[Error] program SumatraPdf is not found\ninform the responsible person\n\n[Ошибка] программа SumatraPdf не найдена\nсообщите ответственному лицу";

    private static readonly string SumatraPathSuffix =
        Path.DirectorySeparatorChar + "SumatraPDF" +
        Path.DirectorySeparatorChar + "SumatraPDF.exe";

    public PrinterViewModel PrinterViewModel { get; } = new();

    private readonly QRCodeGenerator _qrGenerator = new();
    private readonly ConfigFile _configFile;
    private readonly AutoUpdater _autoUpdater;
    private readonly HttpClient _httpClient;
    private bool _socketClose;

    public delegate void RebootHandler();

    public event RebootHandler? Reboot;

    public delegate void PrintAsyncCompleteHandler();

    public event PrintAsyncCompleteHandler? PrintAsyncCompleteEvent;

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

        if (SearchSumatraPdf() == "")
        {
            MessageBox.Show(SumatraError);
            throw new Exception();
        }

        SocketsStartAsync();
    }

    ~PrinterModel()
    {
        _httpClient.Dispose();
        _qrGenerator?.Dispose();
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
        PrinterViewModel.PrintQrVisibility = Visibility.Collapsed;
        if (PrinterViewModel.CodeTextBoxText.ToUpper() == "IDDQD")
        {
            var saveFilePath = _configFile.TempSavePath + Path.DirectorySeparatorChar +
                               "iddqd.pdf";
            saveFilePath =
                saveFilePath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
            ShowComplement();
            await using var s =
                await _httpClient.GetStreamAsync(
                    "https://cdn.profcomff.com/app/printer/iddqd.pdf");
            await using var fs = new FileStream(saveFilePath, FileMode.OpenOrCreate);
            await s.CopyToAsync(fs);
            PrintFile(saveFilePath, new PrintOptions("", 1, false, "A4"),
                patchFrom: "");
            PrinterViewModel.CodeTextBoxText = "";
            PrinterViewModel.DownloadNotInProgress = true;
            Log.Information(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Easter");
            PrintAsyncCompleteEvent?.Invoke();
            return;
        }

        Log.Debug(
            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start response code {PrinterViewModel.CodeTextBoxText}");
        var patchFrom = $"{FileUrl}/{PrinterViewModel.CodeTextBoxText}";
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
        PrintAsyncCompleteEvent?.Invoke();
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

                arguments += ",paper=A4";

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
            if(PrinterViewModel.DownloadNotInProgress)
                PrinterViewModel.PrintQrVisibility = Visibility.Visible;
        }).Start();
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

            Marketing.SocketConnected();
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

        if (websocketReceiveOptions.Reboot)
        {
            Application.Current.Dispatcher.Invoke(() => { Reboot?.Invoke(); });
        }

        if (websocketReceiveOptions.QrToken == null!)
        {
            Marketing.SocketException("websocketReceiveOptions QrToken is null");
            return;
        }

        PrinterViewModel.PrintQr = null!;
        if (websocketReceiveOptions.Files != null!)
        {
            PrinterViewModel.DownloadNotInProgress = false;
            PrinterViewModel.PrintQrVisibility = Visibility.Collapsed;
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

            PrinterViewModel.DownloadNotInProgress = true;
        }

        GenerateQr(websocketReceiveOptions.QrToken);
    }

    private void GenerateQr(string value)
    {
        Log.Debug(
            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: new qr code {value}");
        try
        {
            var qrCodeData = _qrGenerator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new XamlQRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(20, "#FFFFFFFF", "#00FFFFFF", false);
            qrCodeImage.Freeze();
            PrinterViewModel.PrintQr = qrCodeImage;
        }
        catch (Exception exception)
        {
            Log.Error(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: {exception}");
            Marketing.QrGeneratorException(exception.ToString());
            PrinterViewModel.PrintQr = null!;
        }
    }
}