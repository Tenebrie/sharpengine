using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Engine.User.Codegen.Generators;

// TODO: Actually make it work pls
[Generator]
public class MaterialGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        // 1) pick up ALL your .vert.glsl/.frag.glsl files
        var len = 0;
        var str = "";
        ctx.AdditionalTextsProvider.Select((b, c) =>
        {
            len++;
            str += b.Path + "\n";
            return b;
        });
        throw new System.NotImplementedException(len + str);
        var shaders = ctx.AdditionalTextsProvider
            .Where(at => at.Path.EndsWith(".vert.glsl") || at.Path.EndsWith(".frag.glsl"))
            .Select((at, _) =>
            {
                throw new Exception("TESTs");
                var unix = at.Path.Replace("\\", "/");
                var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(unix));
                var dir  = Path.GetDirectoryName(unix).Replace("\\", "/");
                return new { FullPath = unix, Name = name, Dir = dir };
            });

        // 2) collect them and group by directory + name
        var grouped = shaders.Collect();

        // 3) whenever we have both a .vert and a .frag for the same base-name
        ctx.RegisterSourceOutput(grouped, (spc, list) =>
        {
            var byKey = list.GroupBy(x => (x.Dir, x.Name));
            foreach (var grp in byKey)
            {
                bool hasVert = grp.Any(x => x.FullPath.EndsWith(".vert.glsl"));
                bool hasFrag = grp.Any(x => x.FullPath.EndsWith(".frag.glsl"));
                if (!hasVert || !hasFrag) continue;

                // derive class name and shader-key
                string className = grp.Key.Name + "Material";
                // e.g. "Meshes/RawColor/RawColor"
                string shaderKey  = $"{grp.Key.Dir}/{grp.Key.Name}".TrimStart('/');

                // emit the source
                var src = $@"
using Engine.Assets.Materials;

public class {className} : Material
{{
    public {className}() 
        : base(""{shaderKey}"") 
    {{ }}
}}
";
                spc.AddSource($"{className}.g.cs", SourceText.From(src, System.Text.Encoding.UTF8));
            }
        });
    }
}