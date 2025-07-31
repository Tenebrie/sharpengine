using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Assets.Materials;
using Engine.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using static Engine.Bindings.Bgfx.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Assets.Meshes;

public class BoundingSphereMesh
{
    public static BoundingSphereMesh Instance { get; } = new();

    private int _refCount = 0;
        
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private VertexLayout _layout;
    
    public void Load()
    {
        _refCount += 1;
        if (_refCount > 1)
            return;
        
        List<RenderingVertex> verts = [];
        List<int> indices = [];

        const int segmentCount = 64;
        for (var i = 0; i < segmentCount; i++)
        {
            var angle = i * Math.PI * 2 / segmentCount;
            var x = (float)Math.Cos(angle);
            var y = (float)Math.Sin(angle);
            verts.Add(new RenderingVertex(new Vector3(x, 0, y), Color.LightGreen));
            verts.Add(new RenderingVertex(new Vector3(x, y, 0), Color.DeepSkyBlue));
            verts.Add(new RenderingVertex(new Vector3(0, x, y), Color.LightCoral));

            var next = (i + 1) % segmentCount;

            indices.Add((ushort)(3 * i + 0));
            indices.Add((ushort)(3 * next + 0));
            indices.Add((ushort)(3 * i + 1));
            indices.Add((ushort)(3 * next + 1));
            indices.Add((ushort)(3 * i + 2));
            indices.Add((ushort)(3 * next + 2));
        }
        
        var vertsArray = verts.ToArray();
        var indicesArray = new ushort[indices.Count];
        for (var i = 0; i < indices.Count; i++)
            indicesArray[i] = (ushort)indices[i];
        
        CreateVertexLayout(ref _layout, [
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true)
        ]);
        _vertexBuffer = CreateVertexBuffer(ref vertsArray, ref _layout);
        _indexBuffer = CreateIndexBuffer(ref indicesArray);
    }
    
    public static void PrepareRender(uint instanceCount, ref Transform[] worldTransforms, ref RenderContext context)
    {
        for (var i = 0; i < instanceCount; i++)
            worldTransforms[i].ToFloatSpan(
                ref context.InstanceTransformPrepBuffer,
                (int)(context.InstanceTransformCount + i) * context.InstanceTransformStride
            );
        
        context.InstanceTransformCount += instanceCount;
    }
    
    public unsafe void Render(uint instanceCount, Material material, ref RenderContext context)
    {
        if (_refCount == 0)
        {
            Logger.Error("BoundingSphere is not initialized. Call Load() first.");
            return;
        }
        
        var encoder = encoder_begin(false);
        SetInstanceDataBuffer(encoder, context.InstanceTransformBuffer, context.InstanceTransformCount, instanceCount);
        context.InstanceTransformCount += instanceCount;
        
        SetVertexBuffer(encoder, _vertexBuffer);
        SetIndexBuffer(encoder, _indexBuffer);
        SetState(encoder, StateFlags.WriteRgb | StateFlags.WriteZ | StateFlags.DepthTestLess | StateFlags.PtLines);
        
        Submit(encoder, context.ViewId, material.Program, 1, 0);
        
        encoder_end(encoder);
    }

    public void Dereference()
    {
        _refCount -= 1;
        if (_refCount != 0)
            return;
        destroy_vertex_buffer(_vertexBuffer.Handle);
        destroy_index_buffer(_indexBuffer.Handle);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Color color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly uint Color = color.ToAbgr();
    }
}