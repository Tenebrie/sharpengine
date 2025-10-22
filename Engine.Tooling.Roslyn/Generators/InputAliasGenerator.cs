using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.Tooling.Roslyn.Generators;

[Generator]
public sealed class InputAliasGenerator : IIncrementalGenerator
{
    private const string MarkerAttribute = "Engine.Core.Input.Attributes.InputActionsAttribute";

    private sealed class EnumInfo
    {
        public string FqName = "";   // e.g., "global::User.Game.Services.InputAction"
        public long   TypeId;   // stable 64-bit id
    }

    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        // Keep the symbol so we can compute IDs
        var enumInfos =
            ctx.SyntaxProvider.ForAttributeWithMetadataName(
                    MarkerAttribute,
                    predicate: (node, _) => node is EnumDeclarationSyntax,
                    transform: (genCtx, _) => (INamedTypeSymbol)genCtx.TargetSymbol)
               .Where(s => s != null)
               .Select((s, _) => CreateEnumInfo(s!))
               .Collect()
               .Select((list, _) => DeduplicateByFqName(list).ToImmutableArray());

        ctx.RegisterSourceOutput(enumInfos, (spc, infos) =>
        {
            if (infos.IsDefaultOrEmpty) return;

            spc.AddSource("OnInput.derived.g.cs",
                GenerateDerived("OnInputAttribute", "OnBaseInputAttribute", infos));
            spc.AddSource("OnInputHeld.derived.g.cs",
                GenerateDerived("OnInputHeldAttribute", "OnBaseInputHeldAttribute", infos));
            spc.AddSource("OnInputReleased.derived.g.cs",
                GenerateDerived("OnInputReleasedAttribute", "OnBaseInputReleasedAttribute", infos));
        });
    }

    private static List<EnumInfo> DeduplicateByFqName(ImmutableArray<EnumInfo> list)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<EnumInfo>(list.Length);
        foreach (var info in list)
        {
            if (seen.Add(info.FqName))
                result.Add(info);
        }
        return result;
    }

    private static EnumInfo CreateEnumInfo(INamedTypeSymbol symbol)
    {
        var fq = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat); // includes "global::"

        var asm = symbol.ContainingAssembly.Identity;
        var asmKey = new StringBuilder();
        asmKey.Append(asm.Name);
        asmKey.Append('|');
        asmKey.Append(asm.CultureName);
        asmKey.Append('|');
        AppendPublicKeyTokenHex(asmKey, asm.PublicKeyToken);

        // Stable across machines and builds (unless you WANT version churn, then also append asm.Version)
        var key = asmKey.Append("||").Append(fq).ToString();

        return new EnumInfo
        {
            FqName = fq,
            TypeId = StableHash64_Fnv1a(key) // 64-bit
        };
    }

    private static void AppendPublicKeyTokenHex(StringBuilder sb, ImmutableArray<byte> pkt)
    {
        if (pkt.IsDefaultOrEmpty) return;
        foreach (var t in pkt)
        {
            sb.Append(t.ToString("x2"));
        }
    }

    // FNV-1a 64-bit over UTF-8 bytes; deterministic on all platforms
    private static long StableHash64_Fnv1a(string s)
    {
        unchecked
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime  = 1099511628211UL;

            var hash = offset;
            var bytes = Encoding.UTF8.GetBytes(s);
            foreach (var t in bytes)
            {
                hash ^= t;
                hash *= prime;
            }
            return (long)hash;
        }
    }

    private static string GenerateDerived(
        string publicName,
        string baseAttributeName,
        ImmutableArray<EnumInfo> infos)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System;");
        sb.AppendLine("using Silk.NET.Input;");
        sb.AppendLine();
        sb.AppendLine("namespace Engine.Core.Input.Attributes");
        sb.AppendLine("{");
        sb.AppendLine("    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]");
        sb.AppendLine($"    public sealed class {publicName} : {baseAttributeName}");
        sb.AppendLine("    {");

        foreach (var t in infos)
        {
            AppendOverloads(sb, publicName, t);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void AppendOverloads(StringBuilder sb, string publicName, EnumInfo info)
    {
        // locals for brevity
        var t = info.FqName;
        var id = info.TypeId;

        sb.AppendLine($"        public {publicName}({t} action)");
        sb.AppendLine($"            : base({id} + (long)action, 0.0, 0.0, 0.0, InputParamBinding.None) {{ }}");
        sb.AppendLine();

        sb.AppendLine($"        public {publicName}({t} action, int value)");
        sb.AppendLine($"            : base({id} + (long)action, value, 0.0, 0.0, InputParamBinding.Int) {{ }}");
        sb.AppendLine();

        sb.AppendLine($"        public {publicName}({t} action, double value)");
        sb.AppendLine($"            : base({id} + (long)action, value, 0.0, 0.0, InputParamBinding.Double) {{ }}");
        sb.AppendLine();

        sb.AppendLine($"        public {publicName}({t} action, double x, double y)");
        sb.AppendLine($"            : base({id} + (long)action, x, y, 0.0, InputParamBinding.Vector2) {{ }}");
        sb.AppendLine();

        sb.AppendLine($"        public {publicName}({t} action, double x, double y, double z)");
        sb.AppendLine($"            : base({id} + (long)action, x, y, z, InputParamBinding.Vector3) {{ }}");
        sb.AppendLine();
    }
}
