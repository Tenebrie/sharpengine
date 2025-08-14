using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using FontStashSharp;
using FontStashSharp.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = System.Drawing.Color;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using ValueType = Diligent.ValueType;

namespace Engine.Module.Rendering.Fonts;

public class FontRenderer : IFontStashRenderer2, IDisposable
{
    private bool _isValid = false;
    private readonly FontSystem _fontSystem;
    private readonly DynamicSpriteFont _font;
    private IBuffer _vertexBuffer = null!;
    private IBuffer _indexBuffer = null!;

    private const int BufferSizeGlyphs = 8192;
    private MeshPipeline _meshPipeline;

    private Material _material = null!;
    private readonly int _sampleCount;
    private readonly Dictionary<Texture, MaterialInstance> _materialInstances = [];
    private readonly Dictionary<Texture, List<RenderingVertex>> _glyphStream = [];
    
    public ITexture2DManager TextureManager { get; }
    
    public FontRenderer(FontKey key)
    {
        _fontSystem = new FontSystem(new FontSystemSettings
        {
            TextureWidth = 4096,
            TextureHeight = 4096
        });
        _fontSystem.AddFont(File.ReadAllBytes($"Assets/Fonts/{key.Name}.ttf"));
        _font = _fontSystem.GetFont(key.Size * key.SampleCount);
        _sampleCount = key.SampleCount;
        TextureManager = new MyTextureManager(this);
    }

    public unsafe void Initialize()
    {
        _material = Material.CreateFromDisk("UserInterface/Text");
        
        _meshPipeline = PipelineBuilder.PrepareMesh()
            // Position
            .WithLayoutElement(new LayoutElement
            {
                InputIndex = 0,
                NumComponents = 3,
                ValueType = ValueType.Float32,
                IsNormalized = false,
            })
            // UV
            .WithLayoutElement(new LayoutElement
            {
                InputIndex = 1,
                NumComponents = 2,
                ValueType = ValueType.Float32,
                IsNormalized = false,
            })
            // Color
            .WithLayoutElement(new LayoutElement
            {
                InputIndex = 2,
                NumComponents = 4,
                ValueType = ValueType.Float32,
                IsNormalized = false,
            })
            .WithDepthTest(false, false)
            .WithAlphaBlending(true, true)
            .Build();
        
        _vertexBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "FontQuadVertexBuffer",
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.VertexBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            Size = RenderingVertex.SizeInBytes * (ulong)BufferSizeGlyphs * 4 // 4 vertices per quad
        });
        
        // Prefill the index buffer
        var indicesWritten = 0;
        var indices = new ushort[BufferSizeGlyphs * 6];
        for (var glyphIndex = 0; glyphIndex < BufferSizeGlyphs; glyphIndex++)
        {
            var baseIndex = (ushort)(glyphIndex * 4);
            indices[indicesWritten++] = (ushort)(baseIndex + 0);
            indices[indicesWritten++] = (ushort)(baseIndex + 1);
            indices[indicesWritten++] = (ushort)(baseIndex + 2);
            indices[indicesWritten++] = (ushort)(baseIndex + 1);
            indices[indicesWritten++] = (ushort)(baseIndex + 3);
            indices[indicesWritten++] = (ushort)(baseIndex + 2);
        }

        BufferData indexData;
        fixed (ushort* indicesPtr = indices)
        {
            indexData = new BufferData
                { Data = (IntPtr)indicesPtr, DataSize = (uint)(sizeof(ushort) * indices.Length) };
        }
        _indexBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "FontQuadIndexBuffer",
            Usage = Usage.Immutable,
            BindFlags = BindFlags.IndexBuffer,
            CPUAccessFlags = CpuAccessFlags.None,
            Size = (ulong)(Unsafe.SizeOf<ushort>() * BufferSizeGlyphs * 6) // 6 indices per quad
        }, indexData);
        
        _isValid = true;
    }

    public void DrawQuad(
        object texture,
        ref VertexPositionColorTexture topLeftVertex,
        ref VertexPositionColorTexture topRightVertex,
        ref VertexPositionColorTexture bottomLeftVertex,
        ref VertexPositionColorTexture bottomRightVertex)
    {
        var sc = RenderContext.Current.SwapChain.GetDesc();
        var sx = 2f / sc.Width;
        var sy = 2f / sc.Height;

        var tex = (Texture)texture;
        if (!_glyphStream.TryGetValue(tex, out var list))
        {
            list = [];
            _glyphStream[tex] = list;
        }

        var topLeft = topLeftVertex.Position / _sampleCount;
        var width = (topRightVertex.Position - topLeftVertex.Position) / _sampleCount;
        var height = (bottomLeftVertex.Position - topLeftVertex.Position) / _sampleCount;
        var topRight = topLeft + width;
        var bottomLeft = topLeft + height;
        var bottomRight = topLeft + width + height;
        
        list.Add(new RenderingVertex(ToScreenSpace(topLeft), topLeftVertex.TextureCoordinate, topLeftVertex.Color));
        list.Add(new RenderingVertex(ToScreenSpace(topRight), topRightVertex.TextureCoordinate, topRightVertex.Color));
        list.Add(new RenderingVertex(ToScreenSpace(bottomLeft), bottomLeftVertex.TextureCoordinate, bottomLeftVertex.Color));
        list.Add(new RenderingVertex(ToScreenSpace(bottomRight), bottomRightVertex.TextureCoordinate, bottomRightVertex.Color));
        return;

        Vector3 ToScreenSpace(Vector3 p) => new(p.X * sx - 1f, 1f - p.Y * sy, 0f);
    }

    public void RenderText(string text, Vector2 position, Color color, int shadowBlur = 0)
    {
        var fsColor = new FSColor(color.R, color.G, color.B, color.A);
        var renderPos = position * _sampleCount;
        if (shadowBlur > 0)
        {
            var blackColor = new FSColor(0f, 0f, 0f, color.A);
            _font.DrawText(this, text, renderPos, blackColor, effect: FontSystemEffect.Blurry, effectAmount: 4);
        }

        _font.DrawText(this, text, renderPos, fsColor);
    }
    
    public Vector2 MeasureString(string text)
    {
        return _font.MeasureString(text) / _sampleCount;
    }

    public void Flush()
    {
        var verticesWritten = 0;
        var context = RenderContext.Current;
        
        foreach (var (texture, vertexList) in _glyphStream)
        {
            EnsureBufferSize(vertexList.Count);
            
            if (!_materialInstances.TryGetValue(texture, out var materialInstance))
            {
                _materialInstances[texture] = materialInstance = _material.Instantiate();
                materialInstance.LoadTexture(texture);
            }
            
            var pso = AssetManager.Shared.Pipelines.Produce(_meshPipeline, _material.Pipeline);
            context.DeviceContext.SetPipelineState(pso);

            // Vertex buffer
            var vertexBuffer = context.DeviceContext.MapBuffer<RenderingVertex>(_vertexBuffer, MapType.Write, MapFlags.Discard);
            foreach (var vertex in vertexList)
                vertexBuffer[verticesWritten++] = vertex;
            context.DeviceContext.UnmapBuffer(_vertexBuffer, MapType.Write);
            context.DeviceContext.SetVertexBuffers(0, [_vertexBuffer], [0ul], ResourceStateTransitionMode.Transition);
            
            context.DeviceContext.SetIndexBuffer(_indexBuffer, 0, ResourceStateTransitionMode.Transition);

            // Material
            var srb = materialInstance.BindMaterial(pso);
            context.DeviceContext.CommitShaderResources(srb, ResourceStateTransitionMode.Transition);

            context.DeviceContext.DrawIndexed(new DrawIndexedAttribs
            {
                NumIndices = (uint)(vertexList.Count / 4) * 6,
                IndexType = ValueType.UInt16,
            });
        }
        _glyphStream.Clear();
    }
    
    private static void EnsureBufferSize(int vertexCount)
    {
        if (vertexCount < BufferSizeGlyphs * 4)
            return;
        
        throw new ArgumentOutOfRangeException("Too many glyphs to render at once. " +
                                              $"Maximum is {BufferSizeGlyphs}, but got {vertexCount}. " +
                                              "It's time to implement multi-page text rendering!");
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _fontSystem.Dispose();
        if (!_isValid)
            return;
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }

    public void Invalidate(Texture texture)
    {
        _materialInstances.Remove(texture);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Vector2 uv, FSColor color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly Vector2Float Uv = uv.Downgrade();
        public readonly Vector4Float Color = ColorExtensions.ToVector4(color).Downgrade();
        
        public static uint SizeInBytes => (uint)Unsafe.SizeOf<RenderingVertex>();
    }
}

public class MyTextureManager(FontRenderer renderer) : ITexture2DManager
{
    public object CreateTexture(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        return Texture.CreateFromImage(image);
    }

    public Point GetTextureSize(object texture)
    {
        var tex = (Texture)texture;
        return new Point(tex.Width, tex.Height);
    }

    public void SetTextureData(object texture, Rectangle bounds, byte[] data)
    {
        var tex = (Texture)texture;
        tex.Update(data, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
        renderer.Invalidate(tex);
    }
}

public static class ColorExtensions
{
    public static Vector4 ToVector4(this FSColor color)
    {
        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;
        var a = color.A / 255f;
        return new Vector4(r, g, b, a);
    }
}
