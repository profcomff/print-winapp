using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;
using Serilog;

namespace PrinterApp;

public class AutoUpdater
{
    private readonly DispatcherTimer _dispatcherTimer;
    private readonly HttpClient _httpClient;
    private readonly Regex _regex = new(@"v\d+\..+");

    public AutoUpdater()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://github.com/profcomff/print-winapp/releases/latest/"),
        };

        _dispatcherTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _dispatcherTimer.Tick += DispatcherTimer_Tick;

        DeleteOlderExe();
        DeleteOlderZip();
        CreateUpdateBat();
    }

    ~AutoUpdater()
    {
        _httpClient.Dispose();
    }

    public void StartTimer()
    {
        if (_dispatcherTimer.IsEnabled) return;
        Log.Information(
            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Start auto update timer");
        _dispatcherTimer.Start();
    }

    public void StopTimer()
    {
        Log.Information(
            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Stop auto update timer");
        _dispatcherTimer.Stop();
    }

    public void ManualUpdate()
    {
        Log.Information(
            $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Manual update");
        Marketing.ManualUpdate();
        new Task(async () => await CheckNewVersion()).Start();
    }

    private async void DispatcherTimer_Tick(object? sender, EventArgs e)
    {
        var timeNow = DateTime.Now.TimeOfDay;
        var timeNight = new TimeSpan(22, 0, 0);
        var timeEvning = new TimeSpan(6, 0, 0);
        if (timeNow < timeNight && timeNow > timeEvning) return;
        Log.Information(
            $"{GetType().Name} DispatcherTimer_Tick: Auto update timer tick");
        await CheckNewVersion();
    }

    private async Task CheckNewVersion()
    {
        var result = await _httpClient.GetAsync(_httpClient.BaseAddress);
        if (result.IsSuccessStatusCode)
        {
            var requestUri = result.RequestMessage?.RequestUri?.ToString();
            if (requestUri != null)
            {
                var tempVersion = _regex.Match(requestUri).Value[1..].Split(".");
                var version = new int[4];
                for (var i = 0; i < tempVersion.Length; i++)
                {
                    version[i] = int.Parse(tempVersion[i]);
                }

                var githubVersion =
                    new Version(version[0], version[1], version[2], version[3]).ToString();
                var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
                Log.Information(
                    $"{GetType().Name} CheckNewVersion: GithubVersion: {githubVersion} AssemblyVersion: {assemblyVersion}");
                if (githubVersion == assemblyVersion) return;
                //Download new version
                var uri = new Uri(
                    $"https://github.com/profcomff/print-winapp/releases/download/{_regex.Match(requestUri).Value}/PrinterApp_x86.zip");
                var response = await _httpClient.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    Log.Information(
                        $"{GetType().Name} CheckNewVersion: Start download");
                    await using var fs = new FileStream(
                        Path.Combine(
                            Path.GetDirectoryName(Environment.ProcessPath) ??
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "PrinterApp_x86.zip"),
                        FileMode.Create);
                    await response.Content.CopyToAsync(fs);
                    Log.Information(
                        $"{GetType().Name} CheckNewVersion: Finish download");

                    Marketing.UpdateDownloaded();
                    Process.Start(new ProcessStartInfo(BatFileName) { UseShellExecute = false });
                    await Log.CloseAndFlushAsync();
                    Environment.Exit(0);
                }
                else
                {
                    Log.Error($"{GetType().Name} CheckNewVersion: File not found {uri}");
                }
            }
        }
    }

    private static void DeleteOlderExe()
    {
        Log.Information($"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: Start");
        try
        {
            var di = new DirectoryInfo(Path.GetDirectoryName(Environment.ProcessPath) ??
                                       Environment.GetFolderPath(Environment.SpecialFolder
                                           .UserProfile));
            var date = DateTime.Today.AddDays(-14);
            var regex = new Regex(@"_\d+.\d+");
            foreach (var fi in di.GetFiles("PrinterApp_*.exe"))
            {
                var dateAndMonth = regex.Match(fi.Name).Value[1..].Split(".");
                if (date.Month > int.Parse(dateAndMonth[1]))
                {
                    File.Delete(fi.Name);
                }
                else if (date.Month == int.Parse(dateAndMonth[1]) &&
                         date.Day >= int.Parse(dateAndMonth[0]))
                {
                    File.Delete(fi.Name);
                }
            }

            Log.Information($"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: Finish");
        }
        catch (Exception e)
        {
            Log.Error(
                $"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
            Console.WriteLine(
                $"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
        }
    }

    private static void DeleteOlderZip()
    {
        Log.Information($"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: Start");
        try
        {
            var di = new DirectoryInfo(Path.GetDirectoryName(Environment.ProcessPath) ??
                                       Environment.GetFolderPath(Environment.SpecialFolder
                                           .UserProfile));
            foreach (var fi in di.GetFiles("*.zip"))
            {
                File.Delete(fi.Name);
            }

            Log.Information($"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: Finish");
        }
        catch (Exception e)
        {
            Log.Error(
                $"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
            Console.WriteLine(
                $"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
        }
    }

    private const string BatFileName = "update.bat";

    private static void CreateUpdateBat()
    {
        Log.Information($"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: Start");
        try
        {
            string path = Path.Combine(Path.GetDirectoryName(Environment.ProcessPath) ??
                                       Environment.GetFolderPath(Environment.SpecialFolder
                                           .UserProfile), BatFileName);
            using (FileStream fs = File.Create(path))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(BatFile);
                fs.Write(info, 0, info.Length);
            }

            Log.Information($"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: Finish");
        }
        catch (Exception e)
        {
            Log.Error(
                $"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
            Console.WriteLine(
                $"AutoUpdater {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
        }
    }

    private const string BatFile = @"@echo off
echo start
setlocal
timeout 5
for /f ""tokens=2 delims=="" %%a in ('wmic OS Get localdatetime /value') do set ""dt=%%a""
set ""YY=%dt:~2,2%"" & set ""YYYY=%dt:~0,4%"" & set ""MM=%dt:~4,2%"" & set ""DD=%dt:~6,2%""
set ""HH=%dt:~8,2%"" & set ""Min=%dt:~10,2%"" & set ""Sec=%dt:~12,2%""
set ""timestamp=%HH%%Min%%Sec%""
ren ""%~dp0PrinterApp.exe"" PrinterApp_%date%_%timestamp%.exe
call :UnZipFile ""%~dp0"" ""%~dp0PrinterApp_x86.zip""
start PrinterApp.exe
echo close

:UnZipFile <ExtractTo> <newzipfile>
SET vbs=""%temp%\_.vbs""
IF EXIST %vbs% DEL /f /q %vbs%
>>%vbs% ECHO Set fso = CreateObject(""Scripting.FileSystemObject"")
>>%vbs% ECHO If NOT fso.FolderExists(%1) Then
>>%vbs% ECHO fso.CreateFolder(%1)
>>%vbs% ECHO End If
>>%vbs% ECHO set objShell = CreateObject(""Shell.Application"")
>>%vbs% ECHO set FilesInZip = objShell.NameSpace(%2).items
>>%vbs% ECHO objShell.NameSpace(%1).CopyHere FilesInZip, 16
>>%vbs% ECHO Set fso = Nothing
>>%vbs% ECHO Set objShell = Nothing
cscript //nologo %vbs%
IF EXIST %vbs% DEL /f /q %vbs%";
}