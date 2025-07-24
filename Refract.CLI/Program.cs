using DotNetBuddy.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refract.CLI;
using Refract.CLI.Entities;
using Refract.CLI.Services;
using Refract.CLI.Services.Analyzers;
using Refract.CLI.Services.Chunkers;
using Refract.CLI.Services.Embedders;
using Refract.CLI.Services.Indexers;
using Refract.CLI.Services.Talkers;
using Refract.CLI.Views;
using Terminal.Gui.App;

var serviceCollection = new ServiceCollection();
serviceCollection.AddSingleton<IChunker, OverlappingSlidingWindowChunker>();
serviceCollection.AddSingleton<IBinaryAnalyzer, RetDecAnalyzer>();
serviceCollection.AddSingleton<IEmbedder, OllamaEmbedder>();
serviceCollection.AddSingleton<IIndexer, QdrantIndexer>();
serviceCollection.AddSingleton<ITalker, OllamaTalker>();
serviceCollection.AddSingleton<ApplicationContext>();
serviceCollection.AddSingleton<RagService>();
serviceCollection.AddSingleton<MainView>();
serviceCollection.AddBuddy();

// serviceCollection.Configure<OverlappingSlidingWindowChunker.Options>(_ => { });
// serviceCollection.Configure<OllamaEmbedder.Options>(_ => { });
// serviceCollection.Configure<QdrantIndexer.Options>(_ => { });
// serviceCollection.Configure<OllamaTalker.Options>(_ => { });

var serviceProvider = serviceCollection.BuildServiceProvider();
Logging.Logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Global Logger");
Application.Init();

try
{
    Application.Run(serviceProvider.GetService<MainView>()!);
}
finally
{
    Application.Shutdown();
}