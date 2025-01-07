namespace RaggedBooks.Core;

public class RaggedBookConfig
{
    public string PdfFolder { get; set; } = string.Empty;
    public string ChromeExePath { get; set; } = string.Empty;
    public Uri OllamaUrl { get; set; } = new Uri("http://localhost:11434");

    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(PdfFolder))
        {
            throw new InvalidOperationException("PdfFolder is required");
        }
        if (string.IsNullOrWhiteSpace(ChromeExePath))
        {
            throw new InvalidOperationException("ChromeExePath is required");
        }
    }
}
