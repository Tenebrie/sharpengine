using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Plugin.Shaderslang;

public class DocumentColorHandler : IDocumentColorHandler
{
    private readonly BufferManager _bufferManager;
    
    public DocumentColorHandler(BufferManager bufferManager)
    {
        _bufferManager = bufferManager;
        Console.Error.WriteLine("SyntaxHighlightHandler");
    }
    
    public Task<Container<ColorInformation>?> Handle(DocumentColorParams request, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine("HANDLER CALLED 2");
        
        var colors = new List<ColorInformation>();
        colors.Add(new ColorInformation()
        {
            Color = new DocumentColor() { 
                Red = 1.0f,
                Green = 0.2f, 
                Blue = 0.5f, 
                Alpha = 1.0f 
            },
            Range = new()
            {
                Start = new Position(0, 0)
            }
        });
        
        Console.Error.WriteLine("HANDLER CALLED 3333");
        
        return Task.FromResult(new Container<ColorInformation>(colors));
    }

    public DocumentColorRegistrationOptions GetRegistrationOptions(ColorProviderCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentColorRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("shaderslang"),
        };
    }
}