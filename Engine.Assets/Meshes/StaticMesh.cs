using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct RenderingVertex(float x, float y, float z, uint color, Vector3 normal)
{
    public readonly float X = x, Y = y, Z = z;
    public readonly uint Color = color;
    public readonly Vector3 Normal = normal;
}

public enum WindingOrder
{
    Ccw = 0,
    Cw = 1
}

public class StaticMesh : IDisposable
{
    private WindingOrder WindingOrder { get; set; } = WindingOrder.Cw;

    private int _vertexCount;
    private int _indexCount;
    private VertexBufferHandle _vertexBuffer;
    private IndexBufferHandle _indexBuffer;
    private VertexLayout _layout;

    public void Load(AssetVertex[] verts, ushort[] indices)
    {
        var renderVerts = new RenderingVertex[verts.Length];
        for (var i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            const uint a = 0xFF;                                   // full alpha
            var r = (uint)(v.VertexColor.X * 255) & 0xFF;   // Red
            var g = (uint)(v.VertexColor.Y * 255) & 0xFF;   // Green
            var b = (uint)(v.VertexColor.Z * 255) & 0xFF;   // Blue

            var color = (a << 24)
                        | (b << 16)
                        | (g <<  8)
                        |  r;
            renderVerts[i] = new RenderingVertex(v.Position.X, v.Position.Y, v.Position.Z, color, Vector3.One);
        }

        LoadInternal(renderVerts, indices);
    }
    
    private unsafe void LoadInternal(RenderingVertex[] verts, ushort[] indices)
    {
        _vertexCount = verts.Length;
        _indexCount = indices.Length;
        _layout = new VertexLayout();
        fixed (VertexLayout* layout = &_layout)
        {
            vertex_layout_begin(layout, get_renderer_type());
            vertex_layout_add(layout, Attrib.Position, 3, AttribType.Float, true, false);
            vertex_layout_add(layout, Attrib.Color0, 4, AttribType.Uint8, true, true);
            vertex_layout_add(layout, Attrib.Normal, 3, AttribType.Float, true, false);
            vertex_layout_end(layout);

            fixed (RenderingVertex* vPtr = verts)
            fixed (VertexLayout* layoutPtr = &_layout)
            {
                var byteSize = (uint)(verts.Length * sizeof(RenderingVertex));
                _vertexBuffer = create_vertex_buffer(copy(vPtr, byteSize), layoutPtr, 0);
            }
            fixed (ushort* ptr = indices)
            {
                var byteSize = (uint)indices.Length * sizeof(ushort);
                _indexBuffer = create_index_buffer(copy(ptr, byteSize), 0);
            }
        }
    }

    public void LoadUnitCube()
    {
        RenderingVertex[] verts =
        [
            new(-1, -1, -1, 0xff0000ff, Vector3.One), // red
            new( 1, -1, -1, 0xff00ff00, Vector3.One), // green
            new( 1,  1, -1, 0xffffff00, Vector3.One), // yellow
            new(-1,  1, -1, 0xffff0000, Vector3.One), // blue
            new(-1, -1,  1, 0xff00ffff, Vector3.One), // cyan
            new( 1, -1,  1, 0xffff00ff, Vector3.One), // magenta
            new( 1,  1,  1, 0xffffffff, Vector3.One), // white
            new(-1,  1,  1, 0xff808080, Vector3.One)  // gray
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
        
        LoadInternal(verts, indices);
    }

    public unsafe void Render(uint instanceCount, Transform[] worldTransforms, ushort viewId, Material material)
    {
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
        set_instance_data_buffer(&idb, 0, instanceCount);

        set_vertex_buffer(0, _vertexBuffer, 0, (uint)_vertexCount);
        set_index_buffer(_indexBuffer, 0, (uint)_indexCount);
        var stateFlags = StateFlags.WriteRgb | StateFlags.WriteA | StateFlags.WriteZ | StateFlags.DepthTestLess;
        if (WindingOrder == WindingOrder.Ccw)
            stateFlags |= StateFlags.CullCcw;
        else
            stateFlags |= StateFlags.CullCw;
        SetState(stateFlags);

        submit(viewId, material.Program, 1, 0);

    }

    public void Dispose() { destroy_vertex_buffer(_vertexBuffer); destroy_index_buffer(_indexBuffer); }
}