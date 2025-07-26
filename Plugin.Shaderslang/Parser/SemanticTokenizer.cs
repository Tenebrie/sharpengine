using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Plugin.Shaderslang.Parser;

public class SemanticToken(SemanticTokenType type, int length, string value, int linePosition, int charPosition)
{
    public SemanticTokenType Type { get; } = type;
    public string Value { get; } = value;
    public int Length { get; } = length;
    public int LinePosition { get; set; } = linePosition;
    public int CharPosition { get; set; } = charPosition;

    public override string ToString()
    {
        return Type + " token at " + LinePosition + ":" + CharPosition + " " + Value;
    }
}

public class TokenizerError
{
    public required string Message;
    public List<string> AdditionalInformation = [];
    public required int LineIndex;
    public required int CharIndex;
    public required int Length;
    public bool Critical = false;
    
    public override string ToString()
    {
        var additionalInfo = AdditionalInformation.Count > 0 ? " (" + string.Join(", ", AdditionalInformation) + ")" : "";
        return $"{(Critical ? "CRITICAL: " : "")}{Message} at {LineIndex}:{CharIndex} length {Length}{additionalInfo}";
    }
}

[Flags]
public enum GrammaticPatternType : ulong
{
    CommaSeparatedList = 0UL,
    FunctionInvocation = 1UL << 0,
    VariableAssignment = 1UL << 1,
    StandaloneValue = 1UL << 2,
    IncludedFile = 1UL << 3,
    IncludedFilePath = 1UL << 4,
    FunctionDefinition = 1UL << 5,
    FunctionName = 1UL << 6,
    Optional = 1UL << 63,
}

public class GrammaticPattern
{
    public GrammaticPatternType Type { get; set; }
    public SemanticTokenType OutputType;

    private struct RuleDefinition
    {
        public object Type;
        public bool IsRequired;
        public bool IsConsumed;
    }

    private readonly List<RuleDefinition> _ruleDefinitions = [];

    public string ExpectedToken()
    {
        if (_ruleDefinitions.Count == 0)
            return "???";

        return _ruleDefinitions[0].Type.ToString()!;
    }

    private void AddInternal(RuleDefinition rule)
    {
        if (!rule.IsConsumed && _ruleDefinitions.Count == 0)
            throw new ArgumentException("First token of the rule must be consumed", nameof(rule));
        if (rule.IsConsumed && _ruleDefinitions.Count > 0 && !_ruleDefinitions[^1].IsConsumed)
            throw new ArgumentException("Consumed token must not follow a non-consumed token", nameof(rule));
        
        _ruleDefinitions.Add(rule);
    }
    public GrammaticPattern Add(LexicalTokenType type, bool isRequired = true, bool isConsumed = true)
    {
        AddInternal(new RuleDefinition{ Type = type, IsRequired = isRequired, IsConsumed = isConsumed });
        return this;
    }
    public GrammaticPattern Add(GrammaticPatternType type, bool isRequired = true, bool isConsumed = true)
    {
        AddInternal(new RuleDefinition{ Type = type, IsRequired = isRequired, IsConsumed = isConsumed });
        return this;
    }
    public GrammaticPattern Add(string keyword, bool isRequired = true, bool isConsumed = true)
    {
        AddInternal(new RuleDefinition{ Type = keyword, IsRequired = isRequired, IsConsumed = isConsumed });
        return this;
    }

    private GrammaticPattern Assign(SemanticTokenType semanticToken)
    {
        OutputType = semanticToken;
        return this;
    }
    
    public bool StartsWith(LexicalToken token)
    {
        if (_ruleDefinitions.Count == 0)
            return false;
        
        if (token.Type is var tokenType && _ruleDefinitions[0].Type is LexicalTokenType ruleType)
            return tokenType == ruleType;
        if (_ruleDefinitions[0].Type is GrammaticPatternType grammaticRuleType)
            return Vocabulary.GetValueOrDefault(grammaticRuleType)?.Any(r => r.StartsWith(token)) ?? false;
            
        if (_ruleDefinitions[0].Type is string keyword)
            return keyword == token.Value;

        return false;
    }

    private static readonly Dictionary<GrammaticPatternType, List<GrammaticPattern>> Vocabulary = new()
    {
        { GrammaticPatternType.StandaloneValue,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum),
                Make(LexicalTokenType.Keyword)
                    .Assign(SemanticTokenType.Keyword),
                Make(LexicalTokenType.NumericValue)
                    .Assign(SemanticTokenType.Number),
            ]
        },
        { GrammaticPatternType.IncludedFile,
            [
                Make("#include")
                    .Assign(SemanticTokenType.Keyword)
                    .Add(GrammaticPatternType.IncludedFilePath)
            ]
        },
        { GrammaticPatternType.IncludedFilePath,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.String)
            ]
        },
        { GrammaticPatternType.CommaSeparatedList,
            [
                Make(GrammaticPatternType.FunctionInvocation)
                    .Add(LexicalTokenType.Comma)
                    .Add(GrammaticPatternType.CommaSeparatedList),
                Make(GrammaticPatternType.FunctionInvocation)
                    .Add(LexicalTokenType.LineBreak, isConsumed: false),
                Make(GrammaticPatternType.FunctionInvocation)
                    .Add(LexicalTokenType.ParenthesisClose, isConsumed: false),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum)
                    .Add(LexicalTokenType.Comma)
                    .Add(GrammaticPatternType.CommaSeparatedList),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum)
                    .Add(LexicalTokenType.LineBreak, isConsumed: false),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum)
                    .Add(LexicalTokenType.ParenthesisClose, isConsumed: false),
                Make(LexicalTokenType.NumericValue)
                    .Assign(SemanticTokenType.Number)
                    .Add(LexicalTokenType.Comma)
                    .Add(GrammaticPatternType.CommaSeparatedList),
                Make(LexicalTokenType.NumericValue)
                    .Assign(SemanticTokenType.Number)
                    .Add(GrammaticPatternType.CommaSeparatedList, isRequired: false),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum)
            ]
        },
        {
            GrammaticPatternType.FunctionInvocation,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Function)
                    .Add(LexicalTokenType.ParenthesisOpen)
                    .Add(GrammaticPatternType.CommaSeparatedList, isRequired: false)
                    .Add(LexicalTokenType.ParenthesisClose),
                Make(LexicalTokenType.Keyword)
                    .Assign(SemanticTokenType.Function)
                    .Add(LexicalTokenType.ParenthesisOpen)
                    .Add(GrammaticPatternType.CommaSeparatedList, isRequired: false)
                    .Add(LexicalTokenType.ParenthesisClose)
            ]
        },
        {
            GrammaticPatternType.FunctionDefinition,
            [
                Make(LexicalTokenType.Keyword)
                    .Assign(SemanticTokenType.Keyword)
                    .Add(GrammaticPatternType.FunctionName)
                    .Add(LexicalTokenType.ParenthesisOpen)
                    .Add(GrammaticPatternType.CommaSeparatedList, isRequired: false)
                    .Add(LexicalTokenType.ParenthesisClose)
            ]
        },
        {
            GrammaticPatternType.FunctionName,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Function)
            ]
        },
        {
            GrammaticPatternType.VariableAssignment,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Variable)
                    .Add(LexicalTokenType.Equals)
                    .Add(GrammaticPatternType.FunctionInvocation),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Variable)
                    .Add(LexicalTokenType.Equals)
                    .Add(GrammaticPatternType.StandaloneValue),
            ]
        }
    };
    
    static GrammaticPattern()
    {
        foreach (var kvp in Vocabulary)
        {
            foreach (var pattern in kvp.Value)
            {
                pattern.Type = kvp.Key;
            }
        }
    }

    public bool Match(
        List<LexicalToken> tokens,
        int position,
        out int tokensProcessed,
        out List<SemanticToken> results,
        out List<TokenizerError> errors,
        out List<object> matchedPattern,
        int depth = 0)
    {
        results = [];
        errors = [];
        matchedPattern = [];
        var iteratorPos = position;
        tokensProcessed = 0;

        if (tokens.Count <= iteratorPos)
            throw new Exception("Invalid token position");
        
        var token = tokens[iteratorPos];
        
        if (depth >= 30)
        {
            errors.Add(new TokenizerError()
            {
                Message = "Too many nested rule invocations",
                LineIndex = token.LinePosition,
                CharIndex = token.CharPosition,
                Length = token.Length,
            });
            // tokensProcessed += 1;
            return false;
        }

        for (var index = 0; index < _ruleDefinitions.Count; index++)
        {
            var rule = _ruleDefinitions[index];
            
            if (tokens.Count <= iteratorPos)
            {
                errors.Add(new TokenizerError
                {
                    Message = "Not enough tokens to match rule at index " + index + ": " + rule.Type,
                    LineIndex = -1,
                    CharIndex = -1,
                    Length = 1,
                    Critical = rule.IsRequired
                });
                
                if (rule.IsRequired)
                    return false;
                continue;
            }

            token = tokens[iteratorPos];
            switch (rule.Type)
            {
                case LexicalTokenType lexicalTokenType when lexicalTokenType == token.Type:
                {
                    iteratorPos += 1;
                    if (rule.IsConsumed)
                        tokensProcessed += 1;
                    if (index == 0)
                        results.Add(new SemanticToken(OutputType, token.Length, token.Value, token.LinePosition, token.CharPosition));
                    matchedPattern.Add(lexicalTokenType);
                    continue;
                }
                case LexicalTokenType:
                {
                    errors.Add(new TokenizerError
                    {
                        Message = $"Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.",
                        LineIndex = token.CharPosition,
                        CharIndex = token.CharPosition,
                        Length = token.Length,
                        Critical = rule.IsRequired
                    });
                    if (rule.IsRequired)
                        return false;
                    continue;
                }
                case string type when type == token.Value:
                {
                    iteratorPos += 1;
                    if (rule.IsConsumed)
                        tokensProcessed += 1;
                    if (index == 0)
                        results.Add(new SemanticToken(SemanticTokenType.Keyword, token.Length, token.Value, token.LinePosition, token.CharPosition));
                    matchedPattern.Add(type);
                    continue;
                }
                case string:
                {
                    errors.Add(new TokenizerError
                    {
                        Message = $"Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.",
                        LineIndex = token.CharPosition,
                        CharIndex = token.CharPosition,
                        Length = token.Length,
                        Critical = rule.IsRequired
                    });
                    if (rule.IsRequired)
                        return false;
                    continue;
                }
            }

            if (rule.Type is not GrammaticPatternType grammaticRuleType)
                throw new Exception("Unknown rule type: " + rule.Type);

            if (!Vocabulary.TryGetValue(grammaticRuleType, out var subPatterns))
                throw new Exception("Rule is not in the vocabulary: " + rule.Type);

            var matchFound = false;
            var newErrors = new List<TokenizerError>();
            int patternid = 0;
            foreach (var pattern in subPatterns)
            {
                patternid++;
                var didMatch = pattern.Match(
                    tokens,
                    iteratorPos,
                    out var newTokensProcessed,
                    out var newResults,
                    out var newNewErrors,
                    out var newMatchedPattern,
                    depth + 1);
                newErrors.AddRange(newNewErrors);
                if (!didMatch)
                    continue;
                
                matchFound = true;
                results.AddRange(newResults);
                matchedPattern.AddRange(newMatchedPattern);
                iteratorPos += newTokensProcessed;
                tokensProcessed += newTokensProcessed;
                break;
            }

            if (matchFound || !rule.IsRequired)
                continue;
            
            errors.Add(new TokenizerError
            {
                Message = $"Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.",
                LineIndex = token.CharPosition,
                CharIndex = token.CharPosition,
                Length = token.Length,
                Critical = rule.IsRequired
            });
                
            foreach (var newError in newErrors)
            {
                errors.Add(new TokenizerError
                {
                    Message = "> " + newError.Message,
                    LineIndex = newError.LineIndex,
                    CharIndex = newError.CharIndex,
                    Length = newError.Length,
                    Critical = newError.Critical
                });
            }

            return false;
        }

        return tokensProcessed > 0;
    }

    private static GrammaticPattern Make(LexicalTokenType type) => new GrammaticPattern().Add(type);
    private static GrammaticPattern Make(string type) => new GrammaticPattern().Add(type);
    private static GrammaticPattern Make(GrammaticPatternType type) => new GrammaticPattern().Add(type);
}

public abstract class SemanticTokenizer
{
    public static List<SemanticToken> Retokenize(List<LexicalToken> tokens, out List<TokenizerError> errors)
    {
        var output = new List<SemanticToken>();
        errors = [];

        List<(SemanticTokenType, List<object>)> readableRules =
        [
            (SemanticTokenType.Macro,   ["$input", GrammaticPatternType.CommaSeparatedList]),
            (SemanticTokenType.Macro,   ["$output", GrammaticPatternType.CommaSeparatedList]),
            (SemanticTokenType.Keyword, [GrammaticPatternType.IncludedFile]),
            (SemanticTokenType.Keyword, ["---"]),
            (SemanticTokenType.Comment, [LexicalTokenType.Comment]),
            // Function invocation
            (SemanticTokenType.Function, [GrammaticPatternType.FunctionInvocation]),
            // Function definition
            (SemanticTokenType.Keyword, [GrammaticPatternType.FunctionDefinition]),
            // Variable definition
            (SemanticTokenType.Variable,    ["mat4", LexicalTokenType.Identifier, LexicalTokenType.Equals]),
            (SemanticTokenType.Variable,    [LexicalTokenType.Keyword, LexicalTokenType.Identifier, LexicalTokenType.Equals]),
            (SemanticTokenType.Variable,    [LexicalTokenType.Identifier, LexicalTokenType.Identifier, LexicalTokenType.Equals]),
            // Variable assignment
            // (SemanticTokenType.Variable,    [LexicalTokenType.Identifier, LexicalTokenType.Equals, GrammaticRuleType.FunctionInvocation]),
            (SemanticTokenType.Variable,    [GrammaticPatternType.VariableAssignment]),
            // (SemanticTokenType.Variable,    [LexicalTokenType.Identifier, LexicalTokenType.Equals, LexicalTokenType.Identifier]),
        ];

        var rules = new List<(SemanticTokenType, GrammaticPattern)>();
        readableRules.ForEach(r =>
        {
            var key = r.Item1;
            var rule = new GrammaticPattern
            {
                OutputType = r.Item1
            };
            foreach (var @enum in r.Item2)
            {
                switch (@enum)
                {
                    case LexicalTokenType lexicalTokenType:
                        rule.Add(lexicalTokenType);
                        break;
                    case GrammaticPatternType grammaticRuleType:
                        if (grammaticRuleType.HasFlag(GrammaticPatternType.Optional))
                            rule.Add(grammaticRuleType & ~GrammaticPatternType.Optional, isRequired: false);
                        else
                            rule.Add(grammaticRuleType);
                        break;
                    case string keyword:
                        rule.Add(keyword);
                        break;
                }
            }

            rules.Add((key, rule));
        });
        
        var position = 0;
        var lastPosition = -1;
        
        List<LexicalTokenType> resetTokens = [
            LexicalTokenType.LineBreak,
            LexicalTokenType.SemiColon
        ];
        
        while (position < tokens.Count)
        {
            if (lastPosition == position)
            {
                throw new Exception("Infinite loop detected in semantic tokenizer at position " + position + ". Reported errors " + errors.Count);
            }
            lastPosition = position;
            
            var token = tokens[position];
            
            if (token.Type is LexicalTokenType.LineBreak or LexicalTokenType.ScopeOpen or LexicalTokenType.ScopeClose or LexicalTokenType.SemiColon)
            {
                position++;
                continue;
            }
            
            var matchingRules = rules
                .Where(r => r.Item2.StartsWith(token))
                .ToArray();
            if (matchingRules.Length == 0)
            {
                var expectedString = rules.Select(r => r.Item2.ExpectedToken());
                errors.Add(new TokenizerError
                {
                    Message = "Unexpected token: " + token.Value + " (" + token.Type + ")",
                    AdditionalInformation = ["Expected any of the following:", ..expectedString],
                    LineIndex = token.LinePosition,
                    CharIndex = token.CharPosition,
                    Length = token.Length,
                    Critical = true,
                });
                while (position < tokens.Count)
                {
                    position += 1;
                    if (!resetTokens.Contains(tokens[position].Type))
                        continue;
                
                    position += 1;
                    break;
                }
            }

            var matchFound = false;
            var reportedErrors = new List<TokenizerError>();
            foreach (var rule in matchingRules)
            {
                var isMatch = rule.Item2.Match(
                    tokens,
                    position, 
                    out var tokensProcessed,
                    out var newResults,
                    out var newErrors,
                    out var matchedPattern
                );
                foreach (var newError in newErrors)
                {
                    reportedErrors.Add(new TokenizerError
                    {
                        Message = "> " + newError.Message,
                        LineIndex = newError.LineIndex,
                        CharIndex = newError.CharIndex,
                        Length = newError.Length,
                        Critical = newError.Critical
                    });
                }
                if (!isMatch)
                    continue;
                
                // Console.Error.WriteLine("Matched rule: " + rule.Item2.OutputType + " for token: " + token + ", processed " + tokensProcessed + " tokens, " + rule.Item2);
                
                // foreach (var obj in matchedPattern)
                // {
                    // Console.Error.WriteLine(" > " + obj);
                // }
                
                foreach (var semanticToken in newResults)
                {
                    if (semanticToken.Type != null!)
                    {
                        output.Add(semanticToken);
                    }
                }
                position += tokensProcessed;
                matchFound = true;
                break;
            }

            if (matchFound)
                continue;
            
            Console.Error.WriteLine("No matching rule for token: " + token);
            errors.Add(new TokenizerError
            {
                Message = "Unable to match token: " + token,
                LineIndex = token.LinePosition,
                CharIndex = token.CharPosition,
                Length = token.Length,
                Critical = true,
            });
            // if (reportedErrors.Count > 0)
            // {
                // foreach (var error in reportedErrors)
                // {
                    // errors.Add(error);
                    // Console.Error.WriteLine(error);
                // }
            // }
            
            // Skip to the next linebreak
            while (position < tokens.Count)
            {
                position += 1;
                if (!resetTokens.Contains(tokens[position].Type))
                    continue;
                
                position += 1;
                break;
            }
        }

        return output;
    }
}