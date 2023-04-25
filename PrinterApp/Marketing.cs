using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;

namespace PrinterApp;

public static class Marketing
{
#if DEBUG
    private static readonly HttpClient SharedClient = new HttpClient
    {
        BaseAddress = new Uri("https://api.test.profcomff.com/marketing/v1/"),
    };
#else
        private static readonly HttpClient SharedClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.profcomff.com/marketing/v1/"),
        };
#endif
    private static readonly string AssemblyVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    private static void Post(string action, string status, string pathFrom,
        string pathTo)
    {
        var body = new MarketingBody(action: action,
            additional_data:
            $"{{\"status\": \"{status}\",\"app_version\": \"{AssemblyVersion}\"}}",
            path_from: pathFrom, path_to: pathTo);
        SharedClient.PostAsJsonAsync("action", body);
    }

    private static void Post(string action, string status)
    {
        var body = new MarketingBody(action: action,
            additional_data:
            $"{{\"status\": \"{status}\",\"app_version\": \"{AssemblyVersion}\"}}");
        SharedClient.PostAsJsonAsync("action", body);
    }

    private static void Post(string action, string status, float availableMem,
        float currentMem)
    {
        var body = new MarketingBody(action: action,
            additional_data:
            $"{{\"status\": \"{status}\",\"available_mem\": \"{availableMem}\",\"current_mem\": \"{currentMem}\",\"app_version\": \"{AssemblyVersion}\"}}");
        SharedClient.PostAsJsonAsync("action", body);
    }

    public static void StartDownload(string pathFrom,
        string pathTo)
    {
        Post(
            action: "print terminal start download file",
            status: "start_download",
            pathFrom: pathFrom,
            pathTo: pathTo);
    }

    public static void DownloadException(string pathFrom,
        string status)
    {
        Post(
            action: "print terminal download exception",
            status: status,
            pathFrom: pathFrom,
            pathTo: null!);
    }

    public static void FinishDownload(string pathFrom,
        string pathTo)
    {
        Post(
            action: "print terminal finish download file",
            status: "finish_download",
            pathFrom: pathFrom,
            pathTo: pathTo);
    }

    public static void PrintException(string pathFrom,
        string status)
    {
        Post(
            action: "print terminal print exception",
            status: status,
            pathFrom: pathFrom,
            pathTo: null!);
    }

    public static void PrintNotFile(string pathFrom)
    {
        Post(
            action: "print terminal check filename",
            status: "not_file",
            pathFrom: pathFrom,
            pathTo: null!);
    }

    public static void CheckCode(string pathFrom,
        bool statusOk)
    {
        Post(
            action: "print terminal check code",
            status: $"{(statusOk ? "check_code_ok" : "check_code_fail")}",
            pathFrom: pathFrom,
            pathTo: null!);
    }

    public static void StartSumatra(string pathFrom)
    {
        Post(
            action: "print terminal start sumatra",
            status: "start_sumatra",
            pathFrom: pathFrom,
            pathTo: null!);
    }

    public static void LoadProgram()
    {
        Post(
            action: "print terminal load",
            status: "ok");
    }

    public static void MainWindowLoaded()
    {
        Post(
            action: "print terminal main window loaded",
            status: "ok");
    }

    public static void CloseWithoutAccessProgram()
    {
        Post(
            action: "print terminal attempt to close without access",
            status: "ok");
    }

    public static void UpdateDownloaded()
    {
        Post(
            action: "print terminal update download",
            status: "ok");
    }

    public static void ManualUpdate()
    {
        Post(
            action: "print terminal manual update",
            status: "ok");
    }

    public static void ManualReboot()
    {
        Post(
            action: "print terminal manual reboot",
            status: "ok");
    }

    public static void ManualShutdown()
    {
        Post(
            action: "print terminal manual shutdown",
            status: "ok");
    }

    public static void SocketException(string status)
    {
        Post(
            action: "print terminal socket exception",
            status: status);
    }

    public static void SocketConnected()
    {
        Post(
            action: "print terminal socket connected",
            status: "ok");
    }

    public static void MemoryStatus(float availableMem, float currentMem)
    {
        Post(
            action: "print terminal memory status",
            status: "ok",
            availableMem: availableMem,
            currentMem: currentMem);
    }

    public static void MemoryStatusWarning(string status, float availableMem, float currentMem)
    {
        Post(
            action: "print terminal memory status warning",
            status: status,
            availableMem: availableMem,
            currentMem: currentMem);
    }

    public static void QrGeneratorException(string status)
    {
        Post(
            action: "print terminal qr generator exception",
            status: status);
    }
    
    public static void HwndSourceError()
    {
        Post(
            action: "print terminal failed to get window HwndSource",
            status: "error");
    }
}
