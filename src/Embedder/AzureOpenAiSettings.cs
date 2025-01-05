namespace Embedder;

public class AzureOpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;

    public void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new InvalidOperationException("ApiKey is required");
        }
        if (string.IsNullOrWhiteSpace(Endpoint))
        {
            throw new InvalidOperationException("Endpoint is required");
        }
        if (string.IsNullOrWhiteSpace(ModelId))
        {
            throw new InvalidOperationException("ModelId is required");
        }
    }
}
