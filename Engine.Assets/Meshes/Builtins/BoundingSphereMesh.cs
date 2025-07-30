using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Assets.Materials;
using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Assets.Meshes.Builtins;

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
    
    public unsafe void Render(uint instanceCount, ref Transform[] worldTransforms, ushort viewId, Material material)
    {
        if (_refCount == 0)
        {
            Logger.Error("BoundingSphere is not initialized. Call Load() first.");
            return;
        }
        var idb = new InstanceDataBuffer();
        const ushort bytesPerMatrix = 16 * sizeof(float);
        alloc_instance_data_buffer(&idb, instanceCount, bytesPerMatrix);
        
        var instanceDataArray = (float*)idb.data;
        Span<float> mat = stackalloc float[16];
        for (var i = 0; i < instanceCount; i++)
        {
            // var t = Transform.Copy(worldTransforms[i]);
            // t.TranslateLocal(Position);
            // t.Rotation = Quat.Identity;
            // t.Rescale(Radius);
            worldTransforms[i].ToFloatSpan(ref mat);
            for (var j = 0; j < 16; j++)
            {
                instanceDataArray[i * 16 + j] = mat[j];
            }
        }
        SetInstanceDataBuffer(&idb, 0, instanceCount);
        
        SetVertexBuffer(_vertexBuffer);
        SetIndexBuffer(_indexBuffer);
        SetState(StateFlags.WriteRgb | StateFlags.WriteZ | StateFlags.DepthTestLess | StateFlags.PtLines);
        
        submit(viewId, material.Program, 1, 0);
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