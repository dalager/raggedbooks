namespace RaggedBooks.Core;

public class RaggedBookConfig
{
    /// <summary>
    /// The URL of the Ollama server
    /// </summary>
    public Uri OllamaUrl { get; set; } = null!;

    /// <summary>
    /// The URL of the Qdrant service to use for vector search.
    /// </summary>
    public Uri QdrantUrl { get; set; } = null!;

    /// <summary>
    /// The embedding model to use for the vector search.
    /// This is what you would pass to the ollama pull command.
    /// </summary>
    public string EmbeddingModel { get; set; } = string.Empty;

    /// <summary>
    /// The number of dimensions in the embedding model.
    /// This should match the number of dimensions in the embedding model.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1024;

    /// <summary>
    /// Used for tuning the textextraction process.
    /// </summary>
    public int MaxTokensPerLine { get; set; } = 300;

    public int MaxTokensPerParagraph { get; set; } = 512;
    public int OverlapTokens { get; set; } = 100;

    /// <summary>
    /// Name of the chat completion model to use when using
    /// a local ChatCompletionModel. This is the thing that
    /// you would pass to the ollama pull command
    /// </summary>
    public string ChatCompletionModel { get; set; } = null!;

    /// <summary>
    /// If true use local ollama for chat completion, otherwise use Azure AI
    /// </summary>
    public bool UseLocalChatModel { get; set; }

    /// <summary>
    /// The folder where PDFs are stored. Used for locating the PDFs to open.
    /// </summary>
    public string PdfFolder { get; set; } = string.Empty;

    /// <summary>
    /// Path to the Chrome executable to use for opening PDFs.
    /// </summary>
    public string ChromeExePath { get; set; } = string.Empty;

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

        if (string.IsNullOrEmpty(ChatCompletionModel))
        {
            throw new InvalidOperationException("ChatCompletionModel is required");
        }
    }
}
