namespace RaggedBooks.Core.TextExtraction;

public record Book(
    string Title,
    List<Page> Pages,
    BookmarkTree BookmarkTree,
    string Authors,
    string Filename
);