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
    private Assembly _assembly = null!;
    private MaterialDomain _domain = MaterialDomain.Mesh;
    private Color _tintColor = Color.White;
    private bool _isSamplingTexture = false;
    private bool _useCache = true;

    public static MaterialBuilder Begin(object key)
    {
        return BeginInternal(key.ToString()!)
            .SetAssembly(Assembly.GetExecutingAssembly());
    }

    public static MaterialBuilder Begin<T>()
    {
        return BeginInternal(typeof(T).ToString())
            .SetAssembly(Assembly.GetCallingAssembly());
    }

    private static MaterialBuilder BeginInternal(object key) => new(key);
    
    private MaterialBuilder SetAssembly(Assembly assembly)
    {
        _assembly = assembly;
        return this;
    }
    public MaterialBuilder SetDomain(MaterialDomain domain)
    {
        _domain = domain;
        return this;
    }
    public MaterialBuilder SetTintColor(Color color)
    {
        _tintColor = color;
        return this;
    }
    public MaterialBuilder SetSamplingTexture(bool sampling)
    {
        _isSamplingTexture = sampling;
        return this;
    }
    public MaterialBuilder SetCacheAutomatically(bool cache)
    {
        _useCache = cache;
        return this;
    }

    private int GetHash()
    {
        return HashCode.Combine(_tintColor, _domain, _isSamplingTexture);
    }

    public Material Compile()
    {
        var hash = GetHash();
        var storageKey = $"Generated.{key}.{hash}";
        if (_useCache && AssetManager.AssemblyShared(_assembly).Materials.TryGet(storageKey, out var existingMaterial))
            return existingMaterial;
        
        // var vertSource = BuildVertShaderSource();
        // var fragSource = BuildFragShaderSource();
        // var varyingSource = ReadTemplateFile("varying.def.sc");
        
        // var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        
        // var srcDir = Path.Combine(tempDir, "src");
        // var outDir = Path.Combine(tempDir, "out");
        // Directory.CreateDirectory(srcDir);
        // Directory.CreateDirectory(outDir);
        
        // var vertShaderPath = Path.Combine(srcDir, "generated.vert.glsl");
        // var fragShaderPath = Path.Combine(srcDir, "generated.frag.glsl");
        // var varyingFilePath = Path.Combine(srcDir, "varying.def.sc");
        // File.WriteAllText(fragShaderPath, fragSource);
        // File.WriteAllText(vertShaderPath, vertSource);
        // File.WriteAllText(varyingFilePath, varyingSource);
        
        // try
        // {
            // PythonShell.RunEngineScript("build-shaders.py", $"--src {srcDir} --out {outDir}");
        // }
        // catch (Exception e)
        // {
            // Logger.ErrorF("Failed to compile material shaders: {0}", e.Message);
            // Console.WriteLine("Generated vert shader at: " + vertShaderPath);
            // Console.WriteLine("Generated frag shader at: " + fragShaderPath);
        // }

        // var fragBinPath = Path.Combine(outDir, "generated");
        var material = Material.CreateFromGenerated("");
        if (_useCache)
            AssetManager.AssemblyShared(_assembly).Materials.Put(storageKey, material);
        return material;
    }

    private string BuildFragShaderSource()
    {
        var baseShader = $"{System.Enum.GetName(_domain)}.tfrag.glsl";
        var template = ReadTemplateFile(baseShader);
        
        template = template.Replace("$base_color", _isSamplingTexture ? "texture2D(s_diffuse, v_uv0)" : "vec4(1.0, 1.0, 1.0, 1.0)");
        // var r = (_tintColor.R / 255f).ToInvariantString();
        // var g = (_tintColor.G / 255f).ToInvariantString();
        // var b = (_tintColor.B / 255f).ToInvariantString();
        // var a = (_tintColor.A / 255f).ToInvariantString();
        // template = template.Replace("$tint", $"vec4({r}, {g}, {b}, {a})");
        return template;
    }

    private string BuildVertShaderSource()
    {
        var baseShader = $"{System.Enum.GetName(_domain)}.tvert.glsl";
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