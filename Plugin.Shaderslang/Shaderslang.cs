using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Server;
using Plugin.Shaderslang.Enums;
using Plugin.Shaderslang.Handlers;

namespace Plugin.Shaderslang;

internal abstract class Program
{
    private static async Task Main(string[] args)
    {
        var server = await LanguageServer.From(options => options
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