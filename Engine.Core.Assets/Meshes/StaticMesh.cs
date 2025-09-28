using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Communication.Signals;
using Engine.Core.Extensions;
using Engine.Core.Filesystem;
using Silk.NET.Core.Loader;
using ValueType = Diligent.ValueType;

namespace Engine.Core.Assets.Meshes;

public enum WindingOrder
{
    Ccw = 0,
    Cw = 1
}

public class StaticMesh : IDisposable
{
    public bool IsValid { get; private set; }

    private IBuffer _vertexBuffer = null!;
    private IBuffer[] _vertexBufferArray = [];
    private IBuffer _indexBuffer = null!;
    public MeshPipeline Pipeline { get; private set; }

    public AssetVertex[] Vertices { get; private set; } = [];
    public uint[] Indices { get; private set; } = [];

    public Signal<AssetVertex[]> OnMeshLoaded { get; } = new();
    public static ValueType IndexType => ValueType.UInt32;

    protected void LoadInternal(AssetVertex[] vertices, uint[] indices, WindingOrder windingOrder)
    {
        Vertices = vertices;
        Indices = indices;

        var renderVertices = new RenderingVertex[vertices.Length];
        for (var i = 0; i < vertices.Length; i++)
        {
            var v = vertices[i];
            renderVertices[i] = new RenderingVertex(v.Position, v.TexCoord, v.VertexColor, new Vector3(0, 1, 0));
        }

        Pipeline = PipelineBuilder.PrepareMesh()
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
                IsNormalized = true,
            })
            // Normal
            .WithLayoutElement(new LayoutElement
            {
                InputIndex = 3,
                NumComponents = 3,
                ValueType = ValueType.Float32,
                IsNormalized = false,
            })
            .WithDepthTest(true, true)
            .WithAlphaBlending(true, true)
            .WithWindingOrder(windingOrder)
            .Build();

        _vertexBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "StaticMesh vertex buffer",
            Usage = Usage.Immutable,
            BindFlags = BindFlags.VertexBuffer,
            Size = RenderingVertex.SizeInBytes * (uint)vertices.Length
        }, renderVertices);
        _vertexBufferArray = [_vertexBuffer];

        _indexBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "StaticMesh index buffer",
            Usage = Usage.Immutable,
            BindFlags = BindFlags.IndexBuffer,
            Size = (ulong)(Unsafe.SizeOf<uint>() * indices.Length)
        }, indices);

        IsValid = true;

        OnMeshLoaded.Emit(vertices);
    }

    private readonly ulong[] _vertexOffsets = [0ul];
    public void BindForRendering()
    {
        RenderContext.Current.ImmediateContext.SetVertexBuffers(0, _vertexBufferArray, _vertexOffsets, ResourceStateTransitionMode.Transition);
        RenderContext.Current.ImmediateContext.SetIndexBuffer(_indexBuffer, 0, ResourceStateTransitionMode.Transition);
    }
    
    public virtual void Dispose()
    {
        Vertices = [];
        Indices = [];
        GC.SuppressFinalize(this);
        if (!IsValid)
            return;
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        IsValid = false;
    }

    public static StaticMesh CreateFromDisk(string path, WindingOrder windingOrder = WindingOrder.Cw)
    {
        var filepath = FileResolver.Resolve(Path.Combine("Assets", path));
        if (AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Meshes.TryGet(filepath, out var mesh))
            return mesh;

        ObjMeshLoader.LoadObj(filepath, out var vertices, out var indices);
        mesh = CreateFromMemoryWithoutCache(vertices, indices, windingOrder);
        AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Meshes.Put(filepath, mesh);
        return mesh;
    }

    public static StaticMesh CreateFromMemoryWithoutCache(AssetVertex[] verts, uint[] indices, WindingOrder windingOrder = WindingOrder.Cw)
    {
        var newMesh = new StaticMesh();
        newMesh.LoadInternal(verts, indices, windingOrder);
        return newMesh;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Vector2 uv, Color color, Vector3 normal)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly Vector2Float Uv = uv.Downgrade();
        public readonly Vector4Float Color = color.ToVector4().Downgrade();
        public readonly Vector3Float Normal = normal.Downgrade();
        
        public static uint SizeInBytes => (uint)Unsafe.SizeOf<RenderingVertex>();
    }
}
