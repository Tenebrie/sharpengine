using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Assets.Loaders;
using Engine.Assets.Materials;
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

    public void Load(AssetVertex[] verts, ushort[] indices)
    {
        WindingOrder = WindingOrder.Cw;
        var renderVerts = new RenderingVertex[verts.Length];
        for (var i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            renderVerts[i] = new RenderingVertex(v.Position, v.TexCoord, v.VertexColor, Vector3.One);
        }

        LoadInternal(renderVerts, indices);
    }
    
    private void LoadInternal(RenderingVertex[] verts, ushort[] indices)
    {
        CreateVertexLayout(ref _layout, [
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.TexCoord0, 2, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true),
            new VertexLayoutAttribute(Attrib.Normal, 3, AttribType.Float, true, false)
        ]);
        _vertexBuffer = CreateVertexBuffer(ref verts, ref _layout);
        _indexBuffer = CreateIndexBuffer(ref indices);

        IsValid = true;
    }

    public void LoadUnitCube()
    {
        AssetVertex[] verts =
        [
            new(new Vector3(-1, -1, -1), Vector2.Zero, Vector3.One, Color.Red),
            new(new Vector3( 1, -1, -1), Vector2.Zero, Vector3.One, Color.Green),
            new(new Vector3( 1,  1, -1), Vector2.Zero, Vector3.One, Color.Yellow),
            new(new Vector3(-1,  1, -1), Vector2.Zero, Vector3.One, Color.Blue),
            new(new Vector3(-1, -1,  1), Vector2.Zero, Vector3.One, Color.Cyan),
            new(new Vector3( 1, -1,  1), Vector2.Zero, Vector3.One, Color.Magenta),
            new(new Vector3( 1,  1,  1), Vector2.Zero, Vector3.One, Color.White),
            new(new Vector3(-1,  1,  1), Vector2.Zero, Vector3.One, Color.Gray)
        ];

        ushort[] indices =
        [
            0,1,2,  2,3,0,
            5,4,7,  7,6,5,
            4,0,3,  3,7,4,
            1,5,6,  6,2,1,
            3,2,6,  6,7,3,
            4,5,1,  1,0,4 
        ];
        WindingOrder = WindingOrder.Ccw;
        Load(verts, indices);
    }
    
    public unsafe void Render(uint instanceCount, ref Transform[] worldTransforms, ushort viewId, Material material)
    {
        if (!IsValid)
        {
            Logger.Error("StaticMesh is not initialized. Call Load() first.");
            return;
        }
        var idb = new InstanceDataBuffer();
        const ushort bytesPerMatrix = 16 * sizeof(float);
        alloc_instance_data_buffer(&idb, instanceCount, bytesPerMatrix);

        var instanceDataArray = (float*)idb.data;
        for (var i = 0; i < instanceCount; i++)
            worldTransforms[i].ToFloatSpan(instanceDataArray, i * 16);
        
        SetInstanceDataBuffer(&idb, 0, instanceCount);

        SetVertexBuffer(_vertexBuffer);
        SetIndexBuffer(_indexBuffer);
        var stateFlags = StateFlags.WriteRgb | StateFlags.WriteA | StateFlags.WriteZ | StateFlags.DepthTestLess;
        if (WindingOrder == WindingOrder.Ccw)
            stateFlags |= StateFlags.CullCcw;
        else
            stateFlags |= StateFlags.CullCw;
        SetState(stateFlags);
        
        material.BindTexture();
        submit(viewId, material.Program, 1, 0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        destroy_vertex_buffer(_vertexBuffer.Handle);
        destroy_index_buffer(_indexBuffer.Handle);
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