using Newtonsoft.Json;
using Serilog;
using System.IO;
using System.Reflection;
using System;

namespace PrinterApp
{
    public class ConfigFile
    {
        [JsonProperty(Required = Required.Always)]
        public string ExitCode { get; set; } = "dyakov";

        [JsonProperty(Required = Required.Always)]
        public string TempSavePath { get; set; } = Path.GetTempPath() + ".printerApp";

        [JsonProperty(Required = Required.Always)]
        public bool StartWithWindows { get; set; } = false;

        public void LoadConfig(string fileName)
        {
            Log.Information($"{GetType().Name} {MethodBase.GetCurrentMethod()?.Name} start");

            var configPath =
                Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    ?.ToString() + Path.DirectorySeparatorChar + $"{fileName}.json";
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