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
        public bool TwoSided { get; }
    }
}