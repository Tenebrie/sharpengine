using System.Text.RegularExpressions;

namespace Plugin.Shaderslang.Tokenizer;

public enum LexicalTokenType
{
    Comma,
    Keyword,
    Identifier,
    NumericValue,
    Comment,
    Equals,
    ParenthesisOpen,
    ParenthesisClose,
    ScopeOpen,
    ScopeClose,
    SemiColon,
    LineBreak,
}

public class LexicalToken(LexicalTokenType type, string value, int linePosition, int charPosition)
{
    public LexicalTokenType Type { get; } = type;
    public string Value { get; } = value.Trim();
    public int Length => value.Length;
    public int LinePosition { get; set; } = linePosition;
    public int CharPosition { get; set; } = charPosition - value.Length;

    public override string ToString()
    {
        if (Type is LexicalTokenType.Comma or LexicalTokenType.LineBreak)
            return Type + " token at " + LinePosition + ":" + CharPosition;
        return Type + " token at " + LinePosition + ":" + CharPosition + " " + Value;
    }
}

public class LexicalTokenizer
{
    public List<LexicalToken> Tokenize(string source)
    {
        List<LexicalToken> tokenizerTokens = [];
        
        var position = 0;
        var linePosition = 0;
        var lineStartPosition = 1;

        var currentToken = string.Empty;
        var isComment = false;

        var numericValueRegex = new Regex("^[0-9][0-9.x]*$");
        var whitespaceRegex = new Regex(@"^\s*$");
        var keywords = new HashSet<string>
        {
            "$input", "$output", "void"
        };
        
        void FlushToken()
        {
            if (currentToken.Length == 0)
                return;
            
            if (keywords.Contains(currentToken))
            {
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.Keyword, currentToken, linePosition, position - lineStartPosition));
            }
            else if (isComment)
            {
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.Comment, currentToken, linePosition, position - lineStartPosition));
            }
            else if (numericValueRegex.Matches(currentToken).Count > 0)
            {
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.NumericValue, currentToken, linePosition, position - lineStartPosition));
            }
            else
            {
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.Identifier, currentToken, linePosition, position - lineStartPosition));
            }
            
            currentToken = string.Empty;
        }

        while (position < source.Length)
        {
            var nextChar = source[position];
            position += 1;
            
            // Handle Windows-style line endings
            if (nextChar is '\r' && position < source.Length && source[position] == '\n')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.LineBreak, "", linePosition, position - lineStartPosition));
                linePosition += 1;
                lineStartPosition = position + 2;
                isComment = false;
                position += 1;
                continue;
            }
            if (nextChar is '\n' or '\r' or '\t')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.LineBreak, "", linePosition, position - lineStartPosition));
                linePosition += 1;
                lineStartPosition = position + 1;
                isComment = false;
                continue;
            }

            if (nextChar == '/' && position < source.Length && source[position] == '/')
            {
                FlushToken();
                isComment = true;
                position += 1;
                currentToken += "//";
                continue;
            }
            if (isComment && nextChar != '\n')
            {
                currentToken += nextChar;
                continue;
            }
            
            if (nextChar == ',')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.Comma, ",", linePosition, position - lineStartPosition));
                continue;
            }
            if (whitespaceRegex.Match(nextChar.ToString()).Success && currentToken.Length == 0)
            {
                // Ignore whitespaces in general
                continue;
            }
            if (whitespaceRegex.Match(nextChar.ToString()).Success)
            {
                FlushToken();
                continue;
            }

            if (nextChar is '=')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.Equals, "=", linePosition, position - lineStartPosition));
                continue;
            }
            
            if (nextChar is '(')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.ParenthesisOpen, "(", linePosition, position - lineStartPosition));
                continue;
            }
            if (nextChar is ')')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.ParenthesisClose, ")", linePosition, position - lineStartPosition));
                continue;
            }
            if (nextChar is '{')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.ScopeOpen, "{", linePosition, position - lineStartPosition));
                continue;
            }
            if (nextChar is '}')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.ScopeClose, "}", linePosition, position - lineStartPosition));
                continue;
            }
            
            if (nextChar is ';')
            {
                FlushToken();
                tokenizerTokens.Add(new LexicalToken(LexicalTokenType.SemiColon, ";", linePosition, position - lineStartPosition));
                continue;
            }
            
            currentToken += nextChar;
        }
        
        // foreach (var tokenizerToken in tokenizerTokens)
        // {
        //     Console.Error.WriteLine("- " + tokenizerToken);
        // }
        return tokenizerTokens;
    }
}