namespace RaggedBooks.Core.Configuration;

public class AzureOpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The model id to use for the Azure OpenAI service.
    /// This is not the modelname, but rather the name of the model deployment.
    /// You might have named it something like "raggedbooks-gpt4o".
    /// </summary>
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
