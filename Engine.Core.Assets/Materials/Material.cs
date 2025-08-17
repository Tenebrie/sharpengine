
using System.IO.Pipelines;
using System.Reflection;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;

namespace Engine.Core.Assets.Materials;

public class Material : IDisposable
{
    public static RenderContext Context { get; set; }
    
    public MaterialPipeline Pipeline { get; }
    
    protected Material(string shaderPath)
    {
        var vertexShader = Context.RenderDevice.CreateShader(new ShaderCreateInfo
        {
            FilePath = $"{shaderPath}.vsh",
            ShaderSourceStreamFactory = Context.ShaderFactory,
            Desc = new ShaderDesc
            {
                Name = $"VertexShader {shaderPath}",
                ShaderType = ShaderType.Vertex,
                UseCombinedTextureSamplers = true
            },
            SourceLanguage = ShaderSourceLanguage.Hlsl
        }, out _);
        if (vertexShader == null)
            throw new InvalidOperationException($"Failed to create vertex shader from {shaderPath}.vsh");
        
        var pixelShader = Context.RenderDevice.CreateShader(new ShaderCreateInfo
        {
            FilePath = $"{shaderPath}.psh",
            ShaderSourceStreamFactory = Context.ShaderFactory,
            Desc = new ShaderDesc
            {
                Name = $"PixelShader {shaderPath}",
                ShaderType = ShaderType.Pixel,
                UseCombinedTextureSamplers = true
            },
            SourceLanguage = ShaderSourceLanguage.Hlsl
        }, out _);
        if (pixelShader == null)
            throw new InvalidOperationException($"Failed to create pixel shader from {shaderPath}.psh");

        Pipeline = PipelineBuilder.PrepareMaterial(shaderPath)
            .WithVertexShader(vertexShader)
            .WithPixelShader(pixelShader)
            .Build();
    }

    public MaterialInstance Instantiate()
    {
        var instance = InstantiateWithoutCache();
        AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Materials.RegisterInstance(instance);
        return instance;
    }

    public MaterialInstance InstantiateWithoutCache() => new(this);

    public static Material CreateFromDisk(string shaderPath)
    {
        if (AssetManager.Shared.Materials.TryGet(shaderPath, out var material))
            return material;
        var newMaterial = new Material(shaderPath);
        AssetManager.Shared.Materials.Put(shaderPath, newMaterial);
        return newMaterial;
    }

    internal static Material CreateFromGenerated(string fullShaderPath)
    {
        // var vertShader = LoadShader(fullShaderPath + ".vert.bin");
        // var fragShader = LoadShader(fullShaderPath + ".frag.bin");
        // var program = CreateProgram(vertShader, fragShader);
        // return new Material(program, vertShader, fragShader);
        return new Material(fullShaderPath);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Pipeline.PixelShader.Dispose();
        Pipeline.VertexShader.Dispose();
        // destroy_program(Program);
        // destroy_uniform(DiffuseTextureHandle);
        // destroy_uniform(TintColorHandle);
    }
}