namespace RaggedBooks.Core;

public class RaggedBookConfig
{
    public string PdfFolder { get; set; } = string.Empty;
    public string ChromeExePath { get; set; } = string.Empty;
    public required Uri OllamaUrl { get; set; }

    public string EmbeddingModel { get; set; } = string.Empty;
    public int EmbeddingDimensions { get; set; } = 1024;
    public int MaxTokensPerLine { get; set; }
    public int MaxTokensPerParagraph { get; set; }
    public int OverlapTokens { get; set; }

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
        if (string.IsNullOrWhiteSpace(EmbeddingModel))
        {
            throw new InvalidOperationException("EmbeddingModel is required");
        }
        if (EmbeddingDimensions <= 0)
        {
            throw new InvalidOperationException("EmbeddingDimensions must be greater than 0");
        }
    }
}
