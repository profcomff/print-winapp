using Newtonsoft.Json;

namespace PrinterApp;

public class WebsocketReceiveOptions
{
    public WebsocketReceiveOptions(string qrToken, FileWithOptions[] files, bool manualUpdate,
        bool reboot)
    {
        QrToken = qrToken;
        Files = files;
        ManualUpdate = manualUpdate;
        Reboot = reboot;
    }

    [JsonProperty("qr_token")] public string QrToken { get; }
    [JsonProperty("files")] public FileWithOptions[] Files { get; }
    [JsonProperty("manual_update")] public bool ManualUpdate { get; }
    [JsonProperty("reboot")] public bool Reboot { get; }
}