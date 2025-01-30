namespace RaggedBooks.Core.TextExtraction;

public record Book(
    string Title,
    List<Page> Pages,
    ChapterPath ChapterPath,
    string Authors,
    string Filename
);