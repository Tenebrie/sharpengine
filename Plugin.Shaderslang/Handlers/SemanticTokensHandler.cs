using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Plugin.Shaderslang.Tokenizer;

namespace Plugin.Shaderslang.Handlers.SemanticTokens;

public class SemanticTokensHandler(BufferManager bufferManager) : ISemanticTokensFullHandler
{
    private BufferManager BufferManager { get; } = bufferManager;

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
            DocumentSelector = TextDocumentSelector.ForLanguage("shaderslang"),
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
    
    class SemanticToken
    {
        public int DeltaLine { get; set; }
        public int DeltaChar { get; set; }
        public int Length { get; set; }
        public string Value { get; set; }
        public SemanticTokenType Type { get; set; }
        public SemanticTokenModifier? Modifiers { get; set; } // modifiers bit mask
        
        public override string ToString()
        {
            return $"{Type} at {DeltaLine}:{DeltaChar} length {Length} {Value}";
        }
    }
    
    public async Task<OmniSharp.Extensions.LanguageServer.Protocol.Models.SemanticTokens?> Handle(SemanticTokensParams request, CancellationToken cancellationToken)
    {
        var source = BufferManager.GetBuffer(request.TextDocument.Uri.Path);
        if (source is null)
        {
            await Console.Error.WriteLineAsync($"No source found for {request.TextDocument.Uri.Path}");
            return null;
        }
        
        var tokenizer = new LexicalTokenizer();
        var rawTokens = tokenizer.Tokenize(source);
        var syntaxTokenizer = new SemanticTokenizer();
        var semanticTokens = syntaxTokenizer.Retokenize(rawTokens);

        List<SemanticToken> tokens = [];

        var lastChar = 0;
        var lastLine = 0;
        foreach (var token in semanticTokens)
        {
            AddToken(token.LinePosition, token.CharPosition, token.Length, token.Value, token.Type);
        }
        
        void AddToken(int line, int ch, int length, string value, SemanticTokenType type)
        {
            // compute line delta
            int dLine = line - lastLine;
            // if same line, subtract last absolute char; otherwise, ch is from column 0
            int dChar = dLine == 0 
                ? ch - lastChar 
                : ch;
            
            tokens.Add(new SemanticToken
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
        
        return new OmniSharp.Extensions.LanguageServer.Protocol.Models.SemanticTokens { Data = SemanticTokensToData(tokens) };
    }

    private static ImmutableArray<int> SemanticTokensToData(List<SemanticToken> tokens)
    {
        var data = new List<int>();
        foreach (var semanticToken in tokens)
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
}