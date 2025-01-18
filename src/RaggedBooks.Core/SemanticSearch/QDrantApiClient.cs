using Qdrant.Client;

namespace RaggedBooks.Core.SemanticSearch;

public class QDrantApiClient(RaggedBookConfig config)
{
    /// <summary>
    /// Get all books in the collection
    /// THIS is QDrant specific using the facet API.
    /// </summary>
    /// <returns></returns>
    public async Task<Dictionary<string, ulong>> GetBooks()
    {
        var qApi = new QdrantClient(config.QdrantUrl.Host);
        var result = await qApi.FacetAsync(config.VectorCollectionname, "Book");

        var booksDictionary = result.Hits.ToDictionary(
            hit => hit.Value.StringValue,
            hit => hit.Count
        );
        return booksDictionary;
    }
}
