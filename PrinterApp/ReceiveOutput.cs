namespace PrinterApp
{
    public class ReceiveOutput
    {
        public ReceiveOutput(string filename, PrintOptions options)
        {
            Filename = filename;
            Options = options;
        }

        public string Filename { get; }
        public PrintOptions Options { get; }
    }
}