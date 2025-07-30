using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Assets.Meshes;

public enum WindingOrder
{
    Ccw = 0,
    Cw = 1
}

public class StaticMesh : IDisposable
{
    public bool IsValid { get; private set; } = false;
    
    private WindingOrder WindingOrder { get; set; } = WindingOrder.Cw;

    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private VertexLayout _layout;

    public AssetVertex[] Vertices { get; set; } = [];
    public ushort[] Indices { get; set; } = [];
    
    private void Load(AssetVertex[] verts, ushort[] indices, WindingOrder windingOrder)
    {
        Vertices = verts;
        Indices = indices;
        
        WindingOrder = windingOrder;
        var renderVerts = new RenderingVertex[verts.Length];
        for (var i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            renderVerts[i] = new RenderingVertex(v.Position, v.TexCoord, v.VertexColor, Vector3.One);
        }
        
        CreateVertexLayout(ref _layout, [
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.TexCoord0, 2, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true),
            new VertexLayoutAttribute(Attrib.Normal, 3, AttribType.Float, true, false)
        ]);
        _vertexBuffer = CreateVertexBuffer(ref renderVerts, ref _layout);
        _indexBuffer = CreateIndexBuffer(ref indices);

        IsValid = true;
    }

    public void PrepareRender(uint instanceCount, ref Transform[] worldTransforms, ref RenderContext context)
    {
        if (!IsValid)
        {
            Logger.Error("StaticMesh is not initialized. Call Load() first.");
            return;
        }
        
        for (var i = 0; i < instanceCount; i++)
            worldTransforms[i].ToFloatSpan(
                ref context.InstanceTransformPrepBuffer,
                (int)(context.InstanceTransformCount + i) * context.InstanceTransformStride
            );
        
        context.InstanceTransformCount += instanceCount;
    }
    
    public void Render(uint instanceCount, MaterialInstance material, ref RenderContext context)
    {
        if (!IsValid)
        {
            Logger.Error("StaticMesh is not initialized. Call Load() first.");
            return;
        }

        SetVertexBuffer(_vertexBuffer);
        SetIndexBuffer(_indexBuffer);
        SetInstanceDataBuffer(context.InstanceTransformBuffer, context.InstanceTransformCount, instanceCount);
        context.InstanceTransformCount += instanceCount;
        
        var stateFlags = StateFlags.WriteRgb | StateFlags.WriteA | StateFlags.WriteZ | StateFlags.DepthTestLess;
        if (WindingOrder == WindingOrder.Ccw)
            stateFlags |= StateFlags.CullCcw;
        else
            stateFlags |= StateFlags.CullCw;
        SetState(stateFlags);
        
        material.BindTexture();
        submit(context.ViewId, material.Program, 1, 0);
    }

    public void Dispose()
    {
        // GC.SuppressFinalize(this);
        // destroy_vertex_buffer(_vertexBuffer.Handle);
        // destroy_index_buffer(_indexBuffer.Handle);
    }
    
    public static StaticMesh CreateFromMemory(AssetVertex[] verts, ushort[] indices, WindingOrder windingOrder = WindingOrder.Cw)
    {
        var mesh = new StaticMesh();
        mesh.Load(verts, indices, windingOrder);
        return mesh;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Vector2 uv, Color color, Vector3 normal)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly Vector2Float Uv = uv.Downgrade();
        public readonly uint Color = color.ToAbgr();
        public readonly Vector3Float Normal = normal.Downgrade();
    }
}