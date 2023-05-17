using Newtonsoft.Json;

namespace PrinterApp;

public class MarketingBody
{
    public class AdditionalData
    {
        public AdditionalData(string status, string app_version, string terminal_user_id)
        {
            this.status = status;
            this.app_version = app_version;
            this.terminal_user_id = terminal_user_id;
        }

        public AdditionalData(string status, float available_mem, float current_mem,
            string app_version, string terminal_user_id) : this(status, app_version,
            terminal_user_id)
        {
            this.available_mem = available_mem;
            this.current_mem = current_mem;
        }

        public string? status { get; }
        public string? app_version { get; }
        public string? terminal_user_id { get; }
        public float? available_mem { get; }
        public float? current_mem { get; }
    }

    public MarketingBody(string action, AdditionalData additional_data)
    {
        this.action = action;
        JsonSerializerSettings jsonSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            StringEscapeHandling = StringEscapeHandling.Default
        };
        this.additional_data =
            JsonConvert.SerializeObject(additional_data, jsonSerializerSettings);
    }

    public MarketingBody(string action, AdditionalData additional_data, string path_from,
        string path_to) : this(action, additional_data)
    {
        this.path_from = path_from;
        this.path_to = path_to;
    }

    public int user_id { get; set; } = -1;
    public string action { get; }
    public string additional_data { get; }
    public string? path_from { get; }
    public string? path_to { get; }
}