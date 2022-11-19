using Newtonsoft.Json;

namespace PrinterApp;

public class WebsocketReceiveOptions
{
    public WebsocketReceiveOptions(string qrToken, FileWithOptions[] files)
    {
        QrToken = qrToken;
        Files = files;
    }

    [JsonProperty("qr_token")] public string QrToken { get; }
    [JsonProperty("files")] public FileWithOptions[] Files { get; }
}