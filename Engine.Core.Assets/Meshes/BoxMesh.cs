using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using static Engine.Native.Bgfx.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Core.Assets.Meshes;

public class BoxMesh
{
    public static BoxMesh Instance { get; } = new();

    private static bool _isLoaded = false;
        
    private static VertexBuffer _vertexBuffer;
    private static IndexBuffer _indexBuffer;
    private VertexLayout _layout;
    
    public void Load()
    {
        if (_isLoaded)
            return;
        _isLoaded = true;
        
        var positions = new[]
        {
            new Vector3(-1, -1, -1),
            new Vector3( 1, -1, -1),
            new Vector3( 1,  1, -1),
            new Vector3(-1,  1, -1),
            new Vector3(-1, -1,  1),
            new Vector3( 1, -1,  1),
            new Vector3( 1,  1,  1),
            new Vector3(-1,  1,  1)
        };
        var verts = positions.Select(pos => new RenderingVertex(pos, Color.LightBlue)).ToArray();
        
        ushort[] indices =
        [
            0,1,2,  2,3,0,
            5,4,7,  7,6,5,
            4,0,3,  3,7,4,
            1,5,6,  6,2,1,
            3,2,6,  6,7,3,
            4,5,1,  1,0,4 
        ];
        
        _layout = CreateVertexLayout([
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true)
        ]);
        _vertexBuffer = CreateVertexBuffer(ref verts, ref _layout);
        _indexBuffer = CreateIndexBuffer(ref indices);
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
    
    public static unsafe void Render(uint instanceCount, Material material, ref RenderContext context)
    {
        if (!_isLoaded)
        {
            Logger.Error("BoundingSphere is not initialized. Call Load() first.");
            context.InstanceTransformCount += instanceCount;
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
        if (!_isLoaded)
            return;
        destroy_index_buffer(_indexBuffer.Handle);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Color color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly uint Color = color.ToAbgr();
    }
}