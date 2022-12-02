using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Reflection;

namespace PrinterApp
{
    public class ConfigFile
    {
        public string ExitCode { get; set; } = "dyakov";
        public string TempSavePath { get; set; } = Path.GetTempPath() + ".printerApp";
        public bool StartWithWindows { get; set; } = false;
        public bool AutoUpdate { get; set; } = true;
        public string AuthorizationToken { get; set; } = "token";

        public void LoadConfig(string fileName)
        {
            Log.Information($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name} start");
            var configPath =
                Path.Combine(
                    Path.GetDirectoryName(Environment.ProcessPath) ??
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    $"{fileName}.json");
            Log.Debug(
                $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: Full config file patch: {configPath}");
            if (File.Exists(configPath))
            {
                try
                {
                    var config =
                        JsonConvert.DeserializeObject<ConfigFile>(File.ReadAllText(configPath));
                    if (config != null)
                    {
                        ExitCode = config.ExitCode;
                        TempSavePath = config.TempSavePath;
                        StartWithWindows = config.StartWithWindows;
                        AutoUpdate = config.AutoUpdate;
                        AuthorizationToken = config.AuthorizationToken;
                        WriteConfig(configPath);
                        Log.Debug("Load from config file\n" +
                                  JsonConvert.SerializeObject(this, Formatting.Indented));
                    }
                }
                catch (Exception e)
                {
                    Log.Error(
                        $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
                    Console.WriteLine(
                        $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: error {e.Message}");
                    WriteConfig(configPath);
                }
            }
            else
            {
                WriteConfig(configPath);
            }

            Log.Information($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: finish");
        }

        private void WriteConfig(string configPath)
        {
            try
            {
                File.WriteAllText(configPath,
                    JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: write error {e.Message}");
                Log.Error(
                    $"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name}: write error {e.Message}");
            }
        }
    }
}