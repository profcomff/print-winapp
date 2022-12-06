namespace PrinterApp
{
    public class FileWithOptions
    {
        public FileWithOptions(string filename, PrintOptions options)
        {
            Filename = filename;
            Options = options;
        }

        public string Filename { get; }
        public PrintOptions Options { get; }
    }
}