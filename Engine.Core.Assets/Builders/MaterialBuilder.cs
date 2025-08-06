using System.Drawing;
using System.Reflection;
using Engine.Core.Assets.Materials;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Shell;

namespace Engine.Core.Assets.Builders;

public enum MaterialDomain
{
    Mesh,
    UserInterface,
}

public class MaterialBuilder(object key)
{
    private object Key = key;
    private MaterialDomain Domain = MaterialDomain.Mesh;
    private Color TintColor = Color.White;
    private bool IsSamplingTexture = false;
    
    public static MaterialBuilder Begin(object key)
    {
        return new MaterialBuilder(key);
    }
    
    public MaterialBuilder SetDomain(MaterialDomain domain)
    {
        Domain = domain;
        return this;
    }
    public MaterialBuilder SetTintColor(Color color)
    {
        TintColor = color;
        return this;
    }
    public MaterialBuilder SetSamplingTexture(bool sampling)
    {
        IsSamplingTexture = sampling;
        return this;
    }

    public int GetHash()
    {
        return HashCode.Combine(TintColor, Domain);
    }

    public Material Compile()
    {
        var vertSource = BuildVertShaderSource();
        var fragSource = BuildFragShaderSource();
        var varyingSource = ReadTemplateFile("varying.def.sc");
        
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        
        var srcDir = Path.Combine(tempDir, "src");
        var outDir = Path.Combine(tempDir, "out");
        Directory.CreateDirectory(srcDir);
        Directory.CreateDirectory(outDir);
        
        var vertShaderPath = Path.Combine(srcDir, "generated.vert.glsl");
        var fragShaderPath = Path.Combine(srcDir, "generated.frag.glsl");
        var varyingFilePath = Path.Combine(srcDir, "varying.def.sc");
        File.WriteAllText(fragShaderPath, fragSource);
        File.WriteAllText(vertShaderPath, vertSource);
        File.WriteAllText(varyingFilePath, varyingSource);

        try
        {
            PythonShell.RunEngineScript("build-shaders.py", $"--src {srcDir} --out {outDir}");
        }
        catch (Exception e)
        {
            Logger.ErrorF("Failed to compile material shaders: {0}", e.Message);
            Console.WriteLine("Generated vert shader at: " + vertShaderPath);
            Console.WriteLine("Generated frag shader at: " + fragShaderPath);
        }

        var fragBinPath = Path.Combine(outDir, "generated");
        var material = Material.CreateFromGenerated(fragBinPath);
        AssetManager.SubmitMaterial(Key, material);
        return material;
    }

    private string BuildFragShaderSource()
    {
        var baseShader = $"{System.Enum.GetName(Domain)}.tfrag.glsl";
        var template = ReadTemplateFile(baseShader);
        
        template = template.Replace("$base_color", IsSamplingTexture ? "texture2D(s_diffuse, v_uv0)" : "vec4(1.0, 1.0, 1.0, 1.0)");
        var r = (TintColor.R / 255f).ToInvariantString();
        var g = (TintColor.G / 255f).ToInvariantString();
        var b = (TintColor.B / 255f).ToInvariantString();
        var a = (TintColor.A / 255f).ToInvariantString();
        template = template.Replace("$tint", $"vec4({r}, {g}, {b}, {a})");
        return template;
    }

    private string BuildVertShaderSource()
    {
        var baseShader = $"{System.Enum.GetName(Domain)}.tvert.glsl";
        var template = ReadTemplateFile(baseShader);
        return template;
    }

    private static string ReadTemplateFile(string fileName)
    {
        var asm = Assembly.GetExecutingAssembly();
        var resourceName = $"Engine.Core.Assets.Builders.Templates.{fileName}";
        using var stream = asm.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
}