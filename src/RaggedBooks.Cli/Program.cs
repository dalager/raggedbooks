#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050
#pragma warning disable SKEXP0070
using Embedder;
using Microsoft.Extensions.DependencyInjection;
using RaggedBooks.Core;

namespace RaggedBooks.Cli;

internal class Program
{
    private static async global::System.Threading.Tasks.Task Main(string[] args)
    {
        var services = ServiceInitialization.CreateServices<RaggedBooksCli>();
        var cli = services.GetRequiredService<RaggedBooksCli>();
        await cli.Run(args);
    }
}
