using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Assets.Materials;
using Engine.Codegen.Bgfx.Unsafe;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Assets.Meshes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal readonly struct Vertex(float x, float y, float z, uint color)
{
    public readonly float X = x, Y = y, Z = z;
    public readonly uint Color = color;
}

public enum WindingOrder
{
    Ccw = 0,
    Cw = 1
}

public sealed class StaticMesh : IDisposable
{
    private WindingOrder WindingOrder { get; set; } = WindingOrder.Ccw;
    
    private VertexBufferHandle _vertexBuffer;
    private IndexBufferHandle _indexBuffer;
    private VertexLayout _layout;

    public unsafe void LoadUnitCube()
    {
        Vertex[] verts =
        [
            new(-1, -1, -1, 0xff0000ff), // red
            new( 1, -1, -1, 0xff00ff00), // green
            new( 1,  1, -1, 0xffffff00), // yellow
            new(-1,  1, -1, 0xffff0000), // blue
            new(-1, -1,  1, 0xff00ffff), // cyan
            new( 1, -1,  1, 0xffff00ff), // magenta
            new( 1,  1,  1, 0xffffffff), // white
            new(-1,  1,  1, 0xff808080)  // gray
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

        _layout = new VertexLayout();
        fixed (VertexLayout* layout = &_layout)
        {
            vertex_layout_begin(layout, get_renderer_type());
            vertex_layout_add(layout, Attrib.Position, 3, AttribType.Float, true, false);
            vertex_layout_add(layout, Attrib.Color0, 4, AttribType.Uint8, true, true);
            vertex_layout_end(layout);

            fixed (Vertex* vPtr = verts)
            fixed (VertexLayout* layoutPtr = &_layout)
            {
                var byteSize = (uint)(verts.Length * sizeof(Vertex));
                _vertexBuffer = create_vertex_buffer(copy(vPtr, byteSize), layoutPtr, 0);
            }
            fixed (ushort* ptr = indices)
            {
                var byteSize = (uint)indices.Length * sizeof(ushort);
                _indexBuffer = create_index_buffer(copy(ptr, byteSize), 0);
            }
        }
    }

    public void Render(Transform worldTransform, ushort viewId, float width, float height, Material material)
    {
        // Raw view matrix: identity (camera at origin, looking down -Z)
        float[] viewMatrix = {
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, -4, 1
        };

        // Raw perspective projection matrix (60 deg FOV)
        float fov = 60.0f * MathF.PI / 180.0f;
        float aspect = width / height;
        float near = 0.1f;
        float far = 100.0f;
        float f = 1.0f / MathF.Tan(fov / 2.0f);

        float[] projMatrix = {
            f / aspect, 0, 0, 0,
            0, f, 0, 0,
            0, 0, (far + near) / (near - far), -1,
            0, 0, (2 * far * near) / (near - far), 0
        };

        Span<float> modelMatrix = stackalloc float[16];
        worldTransform.ToFloatSpan(ref modelMatrix);

        // Upload raw matrix data directly
        unsafe
        {
            fixed (float* viewPtr = viewMatrix)
            fixed (float* projPtr = projMatrix)
            fixed (float* modelPtr = modelMatrix)
            {
                set_view_transform(viewId, viewPtr, projPtr);
                set_transform(modelPtr, 1);
            }
        }

        // -------- Submit draw call -------------------------------------------------
        set_vertex_buffer(0, _vertexBuffer, 0, 8);
        set_index_buffer(_indexBuffer, 0, 36);
        var stateFlags = StateFlags.WriteRgb | StateFlags.WriteA;
        if (WindingOrder == WindingOrder.Ccw)
            stateFlags |= StateFlags.CullCcw;
        else
            stateFlags |= StateFlags.CullCw;
        SetState(stateFlags);

        submit(viewId, material.Program, 1, 0);
    }

    public void Dispose() { destroy_vertex_buffer(_vertexBuffer); destroy_index_buffer(_indexBuffer); }
}