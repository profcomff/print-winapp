namespace PrinterApp;

public class MarketingBody
{
    public MarketingBody(string action, string additional_data, string path_from,
        string path_to)
    {
        this.action = action;
        this.additional_data = additional_data;
        this.path_from = path_from;
        this.path_to = path_to;
    }

    public int user_id { get; set; } = -1;
    public string action { get; }
    public string additional_data { get; }
    public string path_from { get; }
    public string path_to { get; }
}