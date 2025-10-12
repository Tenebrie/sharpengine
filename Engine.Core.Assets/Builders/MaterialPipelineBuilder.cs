using Diligent;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Logging;

namespace Engine.Core.Assets.Builders;

public static class ShaderVariable
{
    public const string AlbedoSampler = "g_Texture";
    public const string ObjectIndex = "g_ObjectIndex";
    public const string InstanceData = "g_InstanceData";
}

public static class PipelineBuilder
{
    public static int MsaaSamples => 8;
    public static Mesh PrepareMesh()
    {
        return new Mesh();
    }
    
    public static Material PrepareMaterial(string key)
    {
        return new Material(key);
    }
    
    public class Mesh
    {
        private GraphicsPipelineDesc _handle = new();
        private readonly List<LayoutElement> _layoutElements = [];
        
        private readonly IncrementalHashWriter _incrementalHash = new();
    
        public Mesh()
        {
            _handle.PrimitiveTopology = PrimitiveTopology.TriangleList;
            _handle.RasterizerDesc = new RasterizerStateDesc { CullMode = CullMode.Back };
            _handle.DepthStencilDesc = new DepthStencilStateDesc { DepthEnable = true };
            _handle.NumRenderTargets = 1;
            _handle.SmplDesc.Count = (byte)MsaaSamples;
            _handle.RTVFormats = [RenderContext.Current.SwapChain.GetDesc().ColorBufferFormat];
            _handle.DSVFormat = RenderContext.Current.SwapChain.GetDesc().DepthBufferFormat;
            _handle.InputLayout = new InputLayoutDesc();
        }
        
        public Mesh WithPrimitiveTopology(PrimitiveTopology topology)
        {
            _handle.PrimitiveTopology = topology;
            _incrementalHash.Write("PrimitiveTopology", topology.ToString());
            return this;
        }
    
        public Mesh WithLayoutElement(LayoutElement layout)
        {
            _layoutElements.Add(layout);
            // ReSharper disable once UsageOfDefaultStructEquality - it's fiiine here
            _incrementalHash.Write("LayoutElement", layout.GetHashCode().ToString());
            return this;
        }
        
        public Mesh WithWindingOrder(WindingOrder windingOrder)
        {
            _handle.RasterizerDesc.CullMode = windingOrder switch
            {
                WindingOrder.Ccw => CullMode.Back,
                WindingOrder.Cw => CullMode.Front,
                _ => CullMode.None
            };
            _incrementalHash.Write("WindingOrder", windingOrder.ToString());
            return this;
        }
        
        public Mesh WithScissorRect(bool enabled = true)
        {
            _handle.RasterizerDesc.ScissorEnable = enabled;
            _incrementalHash.Write("ScissorRect", enabled.ToString());
            return this;
        }
        
        public Mesh WithDepthTest(bool enabled, bool writeEnabled)
        {
            _handle.DepthStencilDesc.DepthEnable = enabled;
            _handle.DepthStencilDesc.DepthWriteEnable = writeEnabled;
            // _hashCode ^= _handle.DepthStencilDesc.GetHashCode();
            _incrementalHash.Write("DepthTest", enabled.ToString());
            _incrementalHash.Write("DepthWrite", writeEnabled.ToString());
            return this;
        }
        
        public Mesh WithAlphaBlending(bool premultiplied, bool alphaToCoverage)
        {
            // Blend: straight vs premultiplied alpha
            var rt0 = new RenderTargetBlendDesc
            {
                BlendEnable      = true,
                SrcBlend         = premultiplied ? BlendFactor.One : BlendFactor.SrcAlpha,
                DestBlend        = BlendFactor.InvSrcAlpha,
                BlendOp          = BlendOperation.Add,
                SrcBlendAlpha    = BlendFactor.One,
                DestBlendAlpha   = BlendFactor.InvSrcAlpha,
                BlendOpAlpha     = BlendOperation.Add,
                RenderTargetWriteMask = ColorMask.All
            };

            _handle.BlendDesc.IndependentBlendEnable = false;
            _handle.BlendDesc.RenderTargets = [rt0];
            _handle.BlendDesc.AlphaToCoverageEnable = alphaToCoverage;
            _incrementalHash.Write("AlphaBlending", premultiplied.ToString());
            _incrementalHash.Write("AlphaToCoverage", alphaToCoverage.ToString());

            return this;
        }

        public MeshPipeline Build()
        {
            _handle.InputLayout.LayoutElements = _layoutElements.ToArray();
            return new MeshPipeline { Desc = _handle, HashCode = _incrementalHash.Current() };
        }
    }

    public class Material
    {
        private MaterialPipeline _handle = new();
        
        private readonly IncrementalHashWriter _incrementalHash = new();

        public Material(string key)
        {
            _incrementalHash.Write("Key", key);
            _handle.Desc.ResourceLayout = new PipelineResourceLayoutDesc
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
        
        public Material WithVertexShader(IShader vertexShader)
        {
            _handle.VertexShader = vertexShader;
            return this;
        }
        public Material WithPixelShader(IShader pixelShader)
        {
            _handle.PixelShader = pixelShader;
            return this;
        }

        public MaterialPipeline Build()
        {
            return _handle with { HashCode = _incrementalHash.Current() };
        }
    }
    

    public static IPipelineState ComposeWithoutCache(MeshPipeline mesh, MaterialPipeline material)
    {
        var pipelineState = RenderContext.Current.RenderDevice.CreateGraphicsPipelineState(new GraphicsPipelineStateCreateInfo
        {
            PSODesc = material.Desc,
            Vs = material.VertexShader,
            Ps = material.PixelShader,
            GraphicsPipeline = mesh.Desc,
        });
        if (pipelineState == null)
            throw new InvalidOperationException("Failed to create pipeline state from mesh and material.");
        pipelineState.GetStaticVariableByName(ShaderType.Vertex, "Constants")?.Set(RenderContext.Current.ViewMatrixBuffer, SetShaderResourceFlags.None);
        return pipelineState;
    }
}

public struct MeshPipeline
{
    public GraphicsPipelineDesc Desc;
    public string HashCode;
}

public struct MaterialPipeline
{
    public PipelineStateDesc Desc;
    public IShader VertexShader;
    public IShader PixelShader;
    public string HashCode;
}
