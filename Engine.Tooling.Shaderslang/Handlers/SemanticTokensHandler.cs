using System.Collections.Immutable;
using Engine.Tooling.Shaderslang.Enums;
using Engine.Tooling.Shaderslang.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Engine.Tooling.Shaderslang.Handlers;

public class SemanticTokensHandler(DocumentManager documentManager) : ISemanticTokensFullHandler
{
    private DocumentManager DocumentManager { get; } = documentManager;
    private ShaderlangParser Parser = new();

    private static List<SemanticTokenType> SupportedTokens =>
    [
        SemanticTokenType.Keyword,
        SemanticTokenType.String,
        SemanticTokenType.Number,
        SemanticTokenType.Comment,
        SemanticTokenType.Variable,
        SemanticTokenType.Parameter,
        SemanticTokenType.Enum,
        SemanticTokenType.Function,
        SemanticTokenType.Macro
    ];

    public SemanticTokensRegistrationOptions GetRegistrationOptions(SemanticTokensCapability capability, ClientCapabilities clientCapabilities)
    {
        return new SemanticTokensRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(nameof(LanguageSyntax.Shaderslang)),
            Legend = new SemanticTokensLegend
            {
                TokenTypes = SupportedTokens,
                TokenModifiers = new[]
                {
                    SemanticTokenModifier.Declaration,
                    SemanticTokenModifier.Definition,
                }
            },
            Full = true,
            Range = false
        };
    }
    
    public async Task<SemanticTokens?> Handle(SemanticTokensParams request, CancellationToken cancellationToken)
    {
        var source = DocumentManager.GetBuffer(request.TextDocument.Uri.Path);
        if (source is null)
        {
            await Console.Error.WriteLineAsync($"No source found for {request.TextDocument.Uri.Path}");
            return null;
        }
        
        var semanticTokens = ShaderlangParser.ParseDocument(source, out var errors);
        return new SemanticTokens { Data = SemanticTokensToData(semanticTokens) };
    }

    private static ImmutableArray<int> SemanticTokensToData(List<SemanticToken> semanticTokens)
    {
        List<OutputToken> outputTokens = [];

        var lastChar = 0;
        var lastLine = 0;
        foreach (var token in semanticTokens)
        {
            AddToken(token.LinePosition, token.CharPosition, token.Length, token.Value, token.Type);
        }
        
        void AddToken(int line, int ch, int length, string value, SemanticTokenType type)
        {
            var dLine = line - lastLine;
            var dChar = dLine == 0 
                ? ch - lastChar 
                : ch;
            
            outputTokens.Add(new OutputToken
            {
                DeltaLine      = dLine,
                DeltaChar      = dChar,
                Length         = length,
                Type           = type, 
                Modifiers      = null,
                Value          = value
            });
            lastChar = ch;
            lastLine = line;
        }
        
        var data = new List<int>();
        foreach (var semanticToken in outputTokens)
        {
            // LSP semantic‐tokens uses deltas:
            data.Add(semanticToken.DeltaLine);
            data.Add(semanticToken.DeltaChar);
            data.Add(semanticToken.Length);
            data.Add(SupportedTokens.IndexOf(semanticToken.Type));
            // TODO: Add modifiers
            data.Add(0);
        }

        return [..data];
    }
    
    private class OutputToken
    {
        public required int DeltaLine { get; init; }
        public required int DeltaChar { get; init; }
        public required int Length { get; init; }
        public required string Value { get; init; }
        public SemanticTokenType Type { get; init; }
        public SemanticTokenModifier? Modifiers { get; set; } // modifiers bit mask
        
        public override string ToString()
        {
            return $"{Type} at {DeltaLine}:{DeltaChar} length {Length} {Value}";
        }
    }
}