namespace Engine.Tooling.Shaderslang.Parser;

public class ShaderlangParser
{
    class ParseResult
    {
        public required List<SemanticToken> Tokens { get; set; }
    }
    
    public static List<SemanticToken> ParseDocument(string documentText, out List<TokenizerError> errors)
    {
        var tokenizer = new LexicalTokenizer();
        var rawTokens = tokenizer.Tokenize(documentText);
        var semanticTokens = SemanticTokenizer.Retokenize(rawTokens, out errors);
        return semanticTokens;
    }
}