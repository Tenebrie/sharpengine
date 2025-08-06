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

public class BoxMesh : StaticMesh
{
    private static BoxMesh Instance { get; } = new();

    private static bool _isLoaded = false;
        
    public static void Load()
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
        
        Instance.Layout = CreateVertexLayout([
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true)
        ]);
        Instance.VertexBuffer = CreateVertexBuffer(ref verts, ref Instance.Layout);
        Instance.IndexBuffer = CreateIndexBuffer(ref indices);
        AssetManager.Meshes.Put("Generated/BoxMesh", Instance);
    }

    public static void PrepareRender(uint instanceCount, ref Transform[] worldTransforms, ref RenderContext context)
    {
        ((StaticMesh)Instance).PrepareRender(instanceCount, ref worldTransforms, ref context);
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
        
        SetVertexBuffer(encoder, Instance.VertexBuffer);
        SetIndexBuffer(encoder, Instance.IndexBuffer);
        SetState(encoder, StateFlags.WriteRgb | StateFlags.WriteZ | StateFlags.DepthTestLess | StateFlags.PtLines);
        
        Submit(encoder, context.ViewId, material.Program, 1, 0);
        
        encoder_end(encoder);
    }

    public static void Dispose()
    {
        ((StaticMesh)Instance).Dispose();
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Color color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly uint Color = color.ToAbgr();
    }
}