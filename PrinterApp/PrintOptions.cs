using Newtonsoft.Json;

namespace PrinterApp;

public class PrintOptions
{
    public PrintOptions(string pages, int copies, bool twoSided, string format)
    {
        Pages = pages;
        Copies = copies;
        TwoSided = twoSided;
        Format = format;
    }

    public string Pages { get; }
    public int Copies { get; }
    [JsonProperty("two_sided")] public bool TwoSided { get; }
    public string Format { get; }
}