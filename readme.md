# RAGged books

This is a small POC project used for searching a local book collection with semantic search.

Note that I have bought these books and the content is not removed from my computer. The embeddings are generated and stored locally on the same computer and not shared or exposed to the internet.

## Requirements

- .NET 9
- Docker

- A running Ollama (<https://ollama.com/>) installation with a pulled `nomic-embed-txt` embedding model. You should be able to open <http://localhost:11434/api/tags> in your browser and see a list of models with the `nomic-embed-txt` model in it.
- A running QDrant Vector store (<https://github.com/qdrant/qdrant>) in docker exposing default ports, (6333,6334). You should be able to open <http://localhost:6333> in your browser.

## Usage

### Importing books

To import books from the `data` folder, run the following command:

```powershell
dotnet run import-folder "..\..\data\"
```

It will

- Extract the text and chapter structure from the books
- Create embeddings vectors for chunked data with Ollama
- Store the chunks with embeddings in the QDrant vector store

### Searching books

To search books, run the following command:

```powershell
dotnet run search "what is an ADR?"
```

It will give you the first result with the book title and the chapter and page where the search query was found.

**With content**
If you want to see the matching content add the `-content` flag:

```powershell
dotnet run search "How do I define coupling vs cohesion?" -content
```

It will output something like this, where the `Key` property is the UUID of the chunk in the QDrant vector store:

```plaintext
Search score: 0,8235746622085571
Key: e96efea5-6cd2-4ef2-bf5e-168cfce20dd1
Book: buildingmicroservices2ndedition
Chapter: Types of Coupling
Page: 65
Content:
Coupling and cohesion are strongly related and, at some level at least, are arguably
the same in that both concepts describe the relationship between things. Cohesion
applies to the relationship between things inside a boundary (a microservice in our
context), whereas coupling describes the relationship between things across a bound‐
ary. There is no absolute best way to organize our code; coupling and cohesion are
just one way to articulate the various trade-offs we make around where we group
code, and why. All we can strive to do is to find the right balance between these two
ideas, one that makes the most sense for your given context and the problems you are
currently facing.
Remember, the world isn’t static—it’s possible that as your system requirements
change, you’ll find reasons to revisit your decisions. Sometimes parts of your system
may be going through so much change that stability might be impossible. We’ll look
at an example of this in Chapter 3 when I share the experiences of the product devel‐
opment team behind Snap CI.
Types of Coupling
You could infer from the preceding overview above that all coupling is bad. That isn’t
strictly true. Ultimately, some coupling in our system will be unavoidable. What we
want to do is reduce how much coupling we have.
```

(The matching book is "Building Microservices" by Sam Newman, <https://samnewman.io/books/building_microservices_2nd_edition/> )

**Open the page in the browser**

If you want to open the referenced page in the book, add the `-open` flag:

```powershell
dotnet run search "Should I mock a third party REST api during development?" -open
```

It will open the pdf file in Chrome with an appended `#page=123` anchor, which should take you to the correct page.

This last part requires you to have put the Chrome executable path in the `Appsettings.json` file.

## QDrant

```powershell
docker run -d --name qdrant -p 6333:6333 -p 6334:6334 qdrant/qdrant:latest
```

http://localhost:6333/dashboard#/welcome

## Ollama

Download or use the docker image from <https://ollama.com/> and pull the `nomic-embed-txt` model.

With docker: <https://ollama.com/blog/ollama-is-now-available-as-an-official-docker-image>
