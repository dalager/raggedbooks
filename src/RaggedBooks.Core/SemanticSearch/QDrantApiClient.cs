using System.Net.Http.Json;

namespace RaggedBooks.Core.SemanticSearch;

public class QDrantApiClient
{
    private readonly RaggedBookConfig _config;

    public QDrantApiClient(RaggedBookConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Get all books in the collection
    /// THIS is QDrant specific using the facet API.
    /// </summary>
    /// <returns></returns>
    public async Task<Dictionary<string, int>> GetBooks()
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = _config.QdrantUrl;
        var payload = new { key = "Book" };

        var response = await httpClient.PostAsJsonAsync("/collections/skcontent/facet", payload);
        response.EnsureSuccessStatusCode();
        var facetResponse = await response.Content.ReadFromJsonAsync<FacetResponse>();

        var booksDictionary =
            facetResponse?.Result?.Hits.ToDictionary(hit => hit.Value, hit => hit.Count)
            ?? new Dictionary<string, int>();
        return booksDictionary;
    }

    public async Task DeleteCollection(string collectionName)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = _config.QdrantUrl;
        var response = await httpClient.DeleteAsync("/collections/skcontent");
        response.EnsureSuccessStatusCode();
    }

    public record FacetResponse(FacetResult Result);

    public record FacetResult(string Status, int Time, FacetHit[] Hits);

    public record FacetHit(string Value, int Count);
}
