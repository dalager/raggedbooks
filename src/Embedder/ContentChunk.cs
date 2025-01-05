using Microsoft.Extensions.VectorData;

public sealed class ContentChunk
{
    [VectorStoreRecordKey]
    public Guid Key { get; set; }

    [VectorStoreRecordData]
    public required string Book { get; set; }

    [VectorStoreRecordData]
    public required string Chapter { get; set; }

    [VectorStoreRecordData]
    public int PageNumber { get; set; }

    [VectorStoreRecordData]
    public required string Content { get; set; }

    //[VectorStoreRecordVector(Dimensions: 1024)] // mxbai-embed-large
    [VectorStoreRecordVector(Dimensions: 768)] //nomic-embed-txt
    public ReadOnlyMemory<float> ContentEmbedding { get; set; }
}
