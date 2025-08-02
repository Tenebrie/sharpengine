using Engine.Tooling.Shaderslang.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;

namespace Engine.Tooling.Shaderslang;

internal abstract class Program
{
    private static async Task Main(string[] args)
    {
        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(options => options
            .WithInput(Console.OpenStandardInput())
            .WithOutput(Console.OpenStandardOutput())
            .ConfigureLogging(logging =>
            {
                logging
                    .ClearProviders()
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole(opts =>
                        opts.LogToStandardErrorThreshold = LogLevel.Trace
                    )
                    .AddDebug()
                    .AddLanguageProtocolLogging();
            })
            .WithServices(ConfigureServices)
            .WithHandler<SemanticTokensHandler>()
            .WithHandler<TextDocumentSyncHandler>()
            .WithHandler<DocumentColorHandler>()
        );
        
        await server.WaitForExit;
    }
    
    static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentManager>();
    }
}