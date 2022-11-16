using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace PrinterApp
{
    public class PrintOptions
    {
        public PrintOptions(string pages, int copies, bool twoSided)
        {
            Pages = pages;
            Copies = copies;
            TwoSided = twoSided;
        }

        public string Pages { get; }
        public int Copies { get; }
        [JsonProperty("two_sided")] public bool TwoSided { get; }
    }
}