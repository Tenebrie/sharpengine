using Engine.Tooling.Shaderslang.Enums;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Engine.Tooling.Shaderslang;

public class DocumentColorHandler : IDocumentColorHandler
{
    private readonly DocumentManager _documentManager;
    
    public DocumentColorHandler(DocumentManager documentManager)
    {
        _documentManager = documentManager;
    }
    
    public Task<Container<ColorInformation>?> Handle(DocumentColorParams request, CancellationToken cancellationToken)
    {
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
        
        return Task.FromResult(new Container<ColorInformation>(colors));
    }

    public DocumentColorRegistrationOptions GetRegistrationOptions(ColorProviderCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new DocumentColorRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(nameof(LanguageSyntax.Shaderslang)),
        };
    }
}