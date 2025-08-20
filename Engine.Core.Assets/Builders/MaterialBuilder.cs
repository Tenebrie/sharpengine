using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Diligent;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Rendering;

namespace Engine.Core.Assets.Builders;

public struct ConstantBufferDesc
{
    public string Name;
    public ReadOnlyMemory<byte> DefaultValue;
    public int SizeInBytes;
}

public class MaterialBuilder
{
    private Texture? _texture = null;
    private string? _shaderPath = null;
    private bool _useCache = true;
    private MaterialPipeline _pipeline = new();
    private readonly List<ConstantBufferDesc> _constantBuffers = [];
    
    private readonly IncrementalHashWriter _incrementalHash = new();

    public static MaterialBuilder BeginFromFilesystem(string path)
    {
        return new MaterialBuilder().SetShaderPath(path);
    }

    private MaterialBuilder()
    {
        _pipeline.Desc.Name = "Unnamed Material";
        _pipeline.Desc.ResourceLayout = new PipelineResourceLayoutDesc
        {
            DefaultVariableType = ShaderResourceVariableType.Static,
            Variables =
            [
                new ShaderResourceVariableDesc
                {
                    ShaderStages = ShaderType.Pixel,
                    Name = ShaderVariable.AlbedoSampler,
                    Type = ShaderResourceVariableType.Mutable
                },
                new ShaderResourceVariableDesc
                {
                    ShaderStages = ShaderType.Vertex,
                    Name = ShaderVariable.ObjectIndex,
                    Type = ShaderResourceVariableType.Dynamic
                },
                new ShaderResourceVariableDesc
                {
                    ShaderStages = ShaderType.Vertex,
                    Name = ShaderVariable.InstanceData,
                    Type = ShaderResourceVariableType.Dynamic
                }
            ],
            ImmutableSamplers =
            [
                new ImmutableSamplerDesc
                {
                    Desc = new SamplerDesc
                    {
                        MinFilter = FilterType.Anisotropic, MagFilter = FilterType.Anisotropic,
                        MipFilter = FilterType.Linear,
                        MaxAnisotropy = 16,
                        AddressU = TextureAddressMode.Clamp,
                        AddressV = TextureAddressMode.Clamp,
                        AddressW = TextureAddressMode.Clamp
                    },
                    SamplerOrTextureName = ShaderVariable.AlbedoSampler,
                    ShaderStages = ShaderType.Pixel
                }
            ]
        };
    }
    
    public MaterialBuilder SetTexture(Texture texture)
    {
        _texture = texture;
        _incrementalHash.Write("Texture", texture.GetHashCode().ToString());
        return this;
    }
    
    private MaterialBuilder SetShaderPath(string shaderPath)
    {
        _shaderPath = shaderPath;
        _incrementalHash.Write("ShaderPath", shaderPath);
        return this;
    }
    
    public MaterialBuilder SetCacheAutomatically(bool cache)
    {
        _useCache = cache;
        return this;
    }

    public MaterialBuilder SetTextureMode(TextureAddressMode addressMode)
    {
        _pipeline.Desc.ResourceLayout.ImmutableSamplers[0].Desc.AddressU = addressMode;
        _pipeline.Desc.ResourceLayout.ImmutableSamplers[0].Desc.AddressV = addressMode;
        _pipeline.Desc.ResourceLayout.ImmutableSamplers[0].Desc.AddressW = addressMode;
        _incrementalHash.Write(addressMode);
        return this;
    }

    public MaterialBuilder WithUniformPixelBuffer<T>(string name, in T defaultValue) where T : unmanaged
    {
        _pipeline.Desc.ResourceLayout.Variables = _pipeline.Desc.ResourceLayout.Variables.Append(
            new ShaderResourceVariableDesc
            {
                ShaderStages = ShaderType.Pixel,
                Name = name,
                Type = ShaderResourceVariableType.Mutable
            }
        ).ToArray();
        var desc = new ConstantBufferDesc
        {
            Name = name,
            DefaultValue = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in defaultValue), 1)).ToArray(),
            SizeInBytes = Unsafe.SizeOf<T>()
        };
        _constantBuffers.Add(desc);
        _incrementalHash.Write("ConstantBuffer", typeof(T).GUID.ToString("N"));
        return this;
    }

    public Material Compile()
    {
        if (_shaderPath == null)
            throw new InvalidOperationException("Shader path must be set before compiling the material.");
        
        var hash = _incrementalHash.Current();
        var storageKey = $"Generated.{hash}";
        if (_useCache && AssetManager.Shared.Materials.TryGet(storageKey, out var existingMaterial))
            return existingMaterial;
        
        if (RenderContext.Current.ShaderFactory == null)
            throw new InvalidOperationException("Shader factory is not initialized. Cannot compile material without shaders.");
        
        var vertexShader = RenderContext.Current.RenderDevice.CreateShader(new ShaderCreateInfo
        {
            FilePath = $"Assets/{_shaderPath}.vsh",
            ShaderSourceStreamFactory = RenderContext.Current.ShaderFactory,
            Desc = new ShaderDesc
            {
                Name = $"VertexShader {_shaderPath}",
                ShaderType = ShaderType.Vertex,
                UseCombinedTextureSamplers = true
            },
            SourceLanguage = ShaderSourceLanguage.Hlsl
        }, out _);
        if (vertexShader == null)
            throw new InvalidOperationException($"Failed to create vertex shader from Assets/{_shaderPath}.vsh");
        
        var pixelShader = RenderContext.Current.RenderDevice.CreateShader(new ShaderCreateInfo
        {
            FilePath = $"Assets/{_shaderPath}.psh",
            ShaderSourceStreamFactory = RenderContext.Current.ShaderFactory,
            Desc = new ShaderDesc
            {
                Name = $"PixelShader {_shaderPath}",
                ShaderType = ShaderType.Pixel,
                UseCombinedTextureSamplers = true
            },
            SourceLanguage = ShaderSourceLanguage.Hlsl
        }, out _);
        if (pixelShader == null)
            throw new InvalidOperationException($"Failed to create pixel shader from Assets/{_shaderPath}.psh");
        
        _pipeline.VertexShader = vertexShader;
        _pipeline.PixelShader = pixelShader;
        _pipeline.HashCode = hash.GetHashCode();
        
        var constantBuffers =  _constantBuffers.Select(bufferDesc =>
        {
            var buffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
            {
                Name = $"ConstantBuffer {bufferDesc.Name}",
                BindFlags = BindFlags.UniformBuffer,
                Size = (ulong)bufferDesc.SizeInBytes,
                Usage = Usage.Dynamic,
                CPUAccessFlags = CpuAccessFlags.Write,
            });
            if (buffer == null)
                throw new InvalidOperationException($"Failed to create constant buffer for {bufferDesc.Name}");
            var map = RenderContext.Current.DeviceContext.MapBuffer<byte>(buffer, MapType.Write, MapFlags.Discard);
            bufferDesc.DefaultValue.Span.CopyTo(map);
            RenderContext.Current.DeviceContext.UnmapBuffer(buffer, MapType.Write);
            return (bufferDesc.Name, buffer);
        }).ToDictionary();
        
        // var material = Material.CreateFromGenerated("Assets/Shaders/cube");
        var material = new Material(_pipeline, _texture, constantBuffers);
        if (_useCache)
            AssetManager.Shared.Materials.Put(storageKey, material);
        return material;
    }

    public MaterialInstance Instantiate()
    {
        return Compile().Instantiate();
    }
}

internal class IncrementalHashWriter
{
    private readonly IncrementalHash _incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    
    internal void Write(byte[] data)
    {
        _incrementalHash.AppendData(data);
    }
    internal void Write(string label, string data)
    {
        var bytes = Encoding.UTF8.GetBytes(label);
        _incrementalHash.AppendData(bytes);
        bytes = Encoding.UTF8.GetBytes(data);
        _incrementalHash.AppendData(bytes);
    }
    internal void Write(TextureAddressMode textureMode)
    {
        Write("TextureMode", System.Enum.GetName(textureMode)!);
    }
    
    internal string Current()
    {
        return Convert.ToBase64String(_incrementalHash.GetCurrentHash());
    }
}