using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Plugin.Shaderslang.Tokenizer;

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

[Flags]
public enum GrammaticRuleType : ulong
{
    CommaSeparatedList = 0UL,
    FunctionInvocation = 1UL << 0,
    VariableAssignment = 1UL << 1,
    StandaloneValue = 1UL << 2,
    Optional = 1UL << 63,
}

public class GrammaticRule
{
    public SemanticTokenType OutputType;
    struct RuleDefinition
    {
        public object Type;
        public bool IsRequired;
    }

    private readonly List<RuleDefinition> _ruleDefinitions = [];

    public GrammaticRule AddRequired(LexicalTokenType type)
    {
        _ruleDefinitions.Add(new RuleDefinition{ Type = type, IsRequired = true });
        return this;
    }
    public GrammaticRule AddRequired(GrammaticRuleType type)
    {
        _ruleDefinitions.Add(new RuleDefinition{ Type = type, IsRequired = true });
        return this;
    }
    public GrammaticRule AddRequired(string keyword)
    {
        _ruleDefinitions.Add(new RuleDefinition{ Type = keyword, IsRequired = true });
        return this;
    }
    public GrammaticRule AddOptional(LexicalTokenType type)
    {
        _ruleDefinitions.Add(new RuleDefinition{ Type = type, IsRequired = false });
        return this;
    }
    public GrammaticRule AddOptional(GrammaticRuleType type)
    {
        _ruleDefinitions.Add(new RuleDefinition{ Type = type, IsRequired = false });
        return this;
    }

    public GrammaticRule Assign(SemanticTokenType semanticToken)
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
        if (_ruleDefinitions[0].Type is GrammaticRuleType grammaticRuleType)
            return Vocabulary.GetValueOrDefault(grammaticRuleType)?.Any(r => r.StartsWith(token)) ?? false;
            
        if (_ruleDefinitions[0].Type is string keyword)
            return keyword == token.Value;

        return false;
    }

    private static readonly Dictionary<GrammaticRuleType, List<GrammaticRule>> Vocabulary = new()
    {
        { GrammaticRuleType.StandaloneValue,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum),
                Make(LexicalTokenType.Keyword)
                    .Assign(SemanticTokenType.Keyword),
                Make(LexicalTokenType.NumericValue)
                    .Assign(SemanticTokenType.Number),
            ]
        },
        { GrammaticRuleType.CommaSeparatedList,
            [
                Make(GrammaticRuleType.FunctionInvocation)
                    .AddRequired(LexicalTokenType.Comma)
                    .AddRequired(GrammaticRuleType.CommaSeparatedList),
                Make(GrammaticRuleType.FunctionInvocation)
                    .AddOptional(GrammaticRuleType.CommaSeparatedList),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum)
                    .AddRequired(LexicalTokenType.Comma)
                    .AddRequired(GrammaticRuleType.CommaSeparatedList),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Enum)
                    .AddOptional(GrammaticRuleType.CommaSeparatedList),
                Make(LexicalTokenType.NumericValue)
                    .Assign(SemanticTokenType.Number)
                    .AddRequired(LexicalTokenType.Comma)
                    .AddRequired(GrammaticRuleType.CommaSeparatedList),
                Make(LexicalTokenType.NumericValue)
                    .Assign(SemanticTokenType.Number)
                    .AddOptional(GrammaticRuleType.CommaSeparatedList),
            ]
        },
        {
            GrammaticRuleType.FunctionInvocation,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Function)
                    .AddRequired(LexicalTokenType.ParenthesisOpen)
                    .AddOptional(GrammaticRuleType.CommaSeparatedList)
                    .AddRequired(LexicalTokenType.ParenthesisClose),
                Make(LexicalTokenType.Keyword)
                    .Assign(SemanticTokenType.Function)
                    .AddRequired(LexicalTokenType.ParenthesisOpen)
                    .AddOptional(GrammaticRuleType.CommaSeparatedList)
                    .AddRequired(LexicalTokenType.ParenthesisClose)
            ]
        },
        {
            GrammaticRuleType.VariableAssignment,
            [
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Variable)
                    .AddRequired(LexicalTokenType.Equals)
                    .AddRequired(GrammaticRuleType.FunctionInvocation),
                Make(LexicalTokenType.Identifier)
                    .Assign(SemanticTokenType.Variable)
                    .AddRequired(LexicalTokenType.Equals)
                    .AddRequired(GrammaticRuleType.StandaloneValue),
            ]
        }
    };

    public bool Match(List<LexicalToken> tokens, int position, out int tokensProcessed, out List<SemanticToken> results, out List<string> errors)
    {
        var iteratorPos = position;
        tokensProcessed = 0;
        
        results = [];
        errors = [];
        
        for (var index = 0; index < _ruleDefinitions.Count; index++)
        {
            var rule = _ruleDefinitions[index];
            if (tokens.Count <= iteratorPos)
            {
                if (rule.IsRequired)
                {
                    errors.Add("Not enough tokens to match rule at index " + index + ": " + rule.Type);
                    return false;
                }
                errors.Add("[Optional] Not enough tokens to match rule at index " + index + ": " + rule.Type);
                continue;
            }

            var token = tokens[iteratorPos];
            if (rule.Type is LexicalTokenType lexicalTokenType && lexicalTokenType == token.Type)
            {
                iteratorPos += 1;
                tokensProcessed += 1;
                if (index == 0)
                    results.Add(new SemanticToken(OutputType, token.Length, token.Value, token.LinePosition, token.CharPosition));
                continue;
            }

            if (rule.Type is LexicalTokenType)
            {
                if (rule.IsRequired)
                {
                    errors.Add($"Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.");
                    return false;
                }
                errors.Add($"[Optional] Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.");
                continue;
            }

            if (rule.Type is string type && type == token.Value)
            {
                iteratorPos += 1;
                tokensProcessed += 1;
                if (index == 0)
                    results.Add(new SemanticToken(SemanticTokenType.Keyword, token.Length, token.Value, token.LinePosition, token.CharPosition));
                continue;
            }

            if (rule.Type is string)
            {
                if (rule.IsRequired)
                {
                    errors.Add($"Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.");
                    return false;
                }
                errors.Add($"[Optional] Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.");
                continue;
            }

            if (rule.Type is not GrammaticRuleType grammaticRuleType)
                throw new Exception("Unknown rule type: " + rule.Type);

            if (!Vocabulary.TryGetValue(grammaticRuleType, out var subRule))
                throw new Exception("Rule is not in the vocabulary: " + rule.Type);

            var matchFound = false;
            var newErrors = new List<string>();
            foreach (var sub in subRule)
            {
                var didMatch = sub.Match(tokens, iteratorPos, out var newTokensProcessed, out var newResults, out var newNewErrors);
                newErrors.AddRange(newNewErrors);
                if (didMatch)
                {
                    matchFound = true;
                    results.AddRange(newResults);
                    iteratorPos += newTokensProcessed;
                    tokensProcessed += newTokensProcessed;
                    break;
                }
            }
            
            if (!matchFound && !rule.IsRequired)
            {
                errors.Add($"[Optional] Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.");
                foreach (var newError in newErrors)
                {
                    errors.Add("> " + newError);
                }
            }

            if (!matchFound && rule.IsRequired)
            {
                errors.Add($"Unexpected token at {token.LinePosition}:{token.CharPosition}. Expected {rule.Type}, got {token.Value}.");
                foreach (var newError in newErrors)
                {
                    errors.Add("> " + newError);
                }
                return false;
            }
        }

        return true;
    }

    public static GrammaticRule Make(LexicalTokenType type) => new GrammaticRule().AddRequired(type);
    public static GrammaticRule Make(GrammaticRuleType type) => new GrammaticRule().AddRequired(type);
}

public class SemanticTokenizer
{
    public List<SemanticToken> Retokenize(List<LexicalToken> tokens)
    {
        var output = new List<SemanticToken>();

        List<(SemanticTokenType, List<object>)> readableRules =
        [
            (SemanticTokenType.Macro,   ["$input", GrammaticRuleType.CommaSeparatedList]),
            (SemanticTokenType.Macro,   ["$output", GrammaticRuleType.CommaSeparatedList]),
            (SemanticTokenType.Keyword, ["#include", LexicalTokenType.Identifier]),
            (SemanticTokenType.Keyword, ["---"]),
            (SemanticTokenType.Comment, [LexicalTokenType.Comment]),
            // Function invocation
            (SemanticTokenType.Function, [GrammaticRuleType.FunctionInvocation]),
            // Function definition
            (SemanticTokenType.Keyword, [
                    LexicalTokenType.Keyword,
                    LexicalTokenType.Identifier,
                    LexicalTokenType.ParenthesisOpen,
                    GrammaticRuleType.CommaSeparatedList | GrammaticRuleType.Optional,
                    LexicalTokenType.ParenthesisClose
            ]),
            // Variable definition
            (SemanticTokenType.Variable,    ["mat4", LexicalTokenType.Identifier, LexicalTokenType.Equals]),
            (SemanticTokenType.Variable,    [LexicalTokenType.Keyword, LexicalTokenType.Identifier, LexicalTokenType.Equals]),
            (SemanticTokenType.Variable,    [LexicalTokenType.Identifier, LexicalTokenType.Identifier, LexicalTokenType.Equals]),
            // Variable assignment
            // (SemanticTokenType.Variable,    [LexicalTokenType.Identifier, LexicalTokenType.Equals, GrammaticRuleType.FunctionInvocation]),
            // (SemanticTokenType.Variable,    [LexicalTokenType.Identifier, LexicalTokenType.Equals, LexicalTokenType.Identifier]),
            (SemanticTokenType.Variable, [GrammaticRuleType.VariableAssignment])
        ];

        var rules = new List<(SemanticTokenType, GrammaticRule)>();
        readableRules.ForEach(r =>
        {
            var key = r.Item1;
            var rule = new GrammaticRule
            {
                OutputType = r.Item1
            };
            foreach (var @enum in r.Item2)
            {
                switch (@enum)
                {
                    case LexicalTokenType lexicalTokenType:
                        rule.AddRequired(lexicalTokenType);
                        break;
                    case GrammaticRuleType grammaticRuleType:
                        if (grammaticRuleType.HasFlag(GrammaticRuleType.Optional))
                            rule.AddOptional(grammaticRuleType & ~GrammaticRuleType.Optional);
                        else
                            rule.AddRequired(grammaticRuleType);
                        break;
                    case string keyword:
                        rule.AddRequired(keyword);
                        break;
                }
            }

            rules.Add((key, rule));
        });
        
        var position = 0;
        while (position < tokens.Count)
        {
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
                Console.Error.WriteLine("[1] No matching rule for token: " + token);
                break;
            }

            var matchFound = false;
            List<string> errors = [];
            foreach (var rule in matchingRules)
            {
                var isMatch = rule.Item2.Match(tokens, position, out var tokensProcessed, out var newResults, out var newErrors);
                foreach (var newError in newErrors)
                {
                    errors.Add("> " + newError);
                }
                if (!isMatch)
                    continue;
                
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

            if (!matchFound)
            {
                Console.Error.WriteLine("[2] No matching rule for token: " + token);
                if (errors.Count > 0)
                {
                    foreach (var error in errors)
                    {
                        Console.Error.WriteLine(error);
                    }
                }
                break;
            }
        }

        return output;
    }
}