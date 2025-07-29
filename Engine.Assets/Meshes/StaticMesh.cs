using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Core.Common;
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
    public readonly BoundingSphere BoundingSphere = new();
    
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
            var a = (uint)v.VertexColor.A & 0xFF;
            var r = (uint)v.VertexColor.R & 0xFF;
            var g = (uint)v.VertexColor.G & 0xFF;
            var b = (uint)v.VertexColor.B & 0xFF;

            var color = (a << 24)
                        | (b << 16)
                        | (g <<  8)
                        |  r;
            renderVerts[i] = new RenderingVertex(
                (float)v.Position.X, (float)v.Position.Y, (float)v.Position.Z,
                new System.Numerics.Vector2((float)v.TexCoord.X, (float)v.TexCoord.Y),
                color,
                System.Numerics.Vector3.One);
        }

        LoadInternal(renderVerts, indices);
        BoundingSphere.Load(verts);
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

        // 12 triangles (36 indices)
        ushort[] indices =
        [
            0,1,2,  2,3,0,   // -Z
            5,4,7,  7,6,5,   // +Z
            4,0,3,  3,7,4,   // -X
            1,5,6,  6,2,1,   // +X
            3,2,6,  6,7,3,   // +Y
            4,5,1,  1,0,4    // -Y
        ];
        WindingOrder = WindingOrder.Ccw;
        
        // LoadInternal(verts, indices);
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
        Span<float> mat = stackalloc float[16];
        for (var i = 0; i < instanceCount; i++)
        {
            worldTransforms[i].ToFloatSpan(ref mat);
            for (var j = 0; j < 16; j++)
            {
                instanceDataArray[i * 16 + j] = mat[j];
            }
        }
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
        destroy_vertex_buffer(_vertexBuffer.Handle);
        destroy_index_buffer(_indexBuffer.Handle);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(float x, float y, float z, System.Numerics.Vector2 uv, uint color, System.Numerics.Vector3 normal)
    {
        public readonly float X = x, Y = y, Z = z;
        public readonly System.Numerics.Vector2 Uv = uv;
        public readonly uint Color = color;
        public readonly System.Numerics.Vector3 Normal = normal;
    }
}