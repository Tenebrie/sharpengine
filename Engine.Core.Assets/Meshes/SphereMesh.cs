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

public class SphereMesh
{
    [Flags]
    public enum ColorMode
    {
        None = 0,
        AxisColor = 1 << 0,
        Collider = 1 << 1
    }
    
    public static SphereMesh Instance { get; } = new();
    public static ColorMode VisibleModes = ColorMode.None;

    private static int _refCount = 0;
        
    private static VertexBuffer _axisColoredVertexBuffer;
    private static VertexBuffer _colliderVertexBuffer;
    private static IndexBuffer _indexBuffer;
    private VertexLayout _layout;
    
    public void Load()
    {
        _refCount += 1;
        if (_refCount > 1)
            return;
        
        List<RenderingVertex> axisColoredVerts = [];
        List<RenderingVertex> colliderVerts = [];
        List<int> indices = [];

        const int segmentCount = 64;
        for (var i = 0; i < segmentCount; i++)
        {
            var angle = i * Math.PI * 2 / segmentCount;
            var x = (float)Math.Cos(angle);
            var y = (float)Math.Sin(angle);
            axisColoredVerts.Add(new RenderingVertex(new Vector3(x, 0, y), Color.LightGreen));
            axisColoredVerts.Add(new RenderingVertex(new Vector3(x, y, 0), Color.DeepSkyBlue));
            axisColoredVerts.Add(new RenderingVertex(new Vector3(0, x, y), Color.LightCoral));
            colliderVerts.Add(new RenderingVertex(new Vector3(x, 0, y), Color.LightBlue));
            colliderVerts.Add(new RenderingVertex(new Vector3(x, y, 0), Color.LightBlue));
            colliderVerts.Add(new RenderingVertex(new Vector3(0, x, y), Color.LightBlue));

            var next = (i + 1) % segmentCount;

            indices.Add((ushort)(3 * i + 0));
            indices.Add((ushort)(3 * next + 0));
            indices.Add((ushort)(3 * i + 1));
            indices.Add((ushort)(3 * next + 1));
            indices.Add((ushort)(3 * i + 2));
            indices.Add((ushort)(3 * next + 2));
        }
        
        var vertsArray = axisColoredVerts.ToArray();
        var colliderVertsArray = colliderVerts.ToArray();
        var indicesArray = new ushort[indices.Count];
        for (var i = 0; i < indices.Count; i++)
            indicesArray[i] = (ushort)indices[i];
        
        _layout = CreateVertexLayout([
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true)
        ]);
        _axisColoredVertexBuffer = CreateVertexBuffer(ref vertsArray, ref _layout);
        _colliderVertexBuffer = CreateVertexBuffer(ref colliderVertsArray, ref _layout);
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
    
    public static unsafe void Render(uint instanceCount, Material material, ref RenderContext context, ColorMode color)
    {
        if (VisibleModes == ColorMode.None || (VisibleModes & color) == 0)
        {
            context.InstanceTransformCount += instanceCount;
            return;
        }

        if (_refCount == 0)
        {
            Logger.Error("BoundingSphere is not initialized. Call Load() first.");
            context.InstanceTransformCount += instanceCount;
            return;
        }
        
        var encoder = encoder_begin(false);
        SetInstanceDataBuffer(encoder, context.InstanceTransformBuffer, context.InstanceTransformCount, instanceCount);
        context.InstanceTransformCount += instanceCount;
        
        if (color == ColorMode.AxisColor)
            SetVertexBuffer(encoder, _axisColoredVertexBuffer);
        else if (color == ColorMode.Collider)
            SetVertexBuffer(encoder, _colliderVertexBuffer);
        else
            throw new ArgumentOutOfRangeException(nameof(color), color, null);
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
        destroy_vertex_buffer(_axisColoredVertexBuffer.Handle);
        destroy_index_buffer(_indexBuffer.Handle);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Color color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly uint Color = color.ToAbgr();
    }
}