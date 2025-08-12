using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Core.Logging;

namespace Engine.Core.Assets.Meshes;

public class LineSphereMesh : StaticMesh
{
    [Flags]
    public enum ColorMode
    {
        None = 0,
        AxisColor = 1 << 0,
        Collider = 1 << 1
    }
    
    public static ColorMode VisibleModes = ColorMode.None;
    private static readonly LineSphereMesh Instance = new();
    public static LineSphereMesh Shared => Instance.Load(Assembly.GetCallingAssembly());

    private bool _isLoaded = false;

    // private VertexBuffer _axisColoredVertexBuffer = VertexBuffer.Invalid;
    // private VertexBuffer _colliderVertexBuffer = VertexBuffer.Invalid;
    
    private LineSphereMesh Load(Assembly callingAssembly)
    {
        if (_isLoaded)
            return this;
        _isLoaded = true;
        
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
        
        // Layout = CreateVertexLayout([
            // new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            // new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true)
        // ]);
        // _axisColoredVertexBuffer = CreateVertexBuffer(ref vertsArray, ref Layout);
        // _colliderVertexBuffer = CreateVertexBuffer(ref colliderVertsArray, ref Layout);
        // IndexBuffer = CreateIndexBuffer(ref indicesArray);
        AssetManager.AssemblyShared(callingAssembly).Meshes.Put("Generated/LineSphereMesh", this);
        return this;
    }
    
    public unsafe void Render(uint instanceCount, Transform[] worldTransforms, MaterialInstance[] materials, ColorMode color)
    {
        // if (VisibleModes == ColorMode.None || (VisibleModes & color) == 0)
        // {
        //     context.InstanceTransformCount += instanceCount;
        //     return;
        // }
        //
        // if (!_isLoaded)
        // {
        //     Logger.Error("BoundingSphere is not initialized. Call Load() first.");
        //     context.InstanceTransformCount += instanceCount;
        //     return;
        // }
        
        // var encoder = encoder_begin(false);
        // SetInstanceDataBuffer(encoder, context.InstanceTransformBuffer, context.InstanceTransformCount, instanceCount);
        // context.InstanceTransformCount += instanceCount;
        
        // if (color == ColorMode.AxisColor)
            // SetVertexBuffer(encoder, _axisColoredVertexBuffer);
        // else if (color == ColorMode.Collider)
            // SetVertexBuffer(encoder, _colliderVertexBuffer);
        // else
            // throw new ArgumentOutOfRangeException(nameof(color), color, null);
        // SetIndexBuffer(encoder, IndexBuffer);
        // SetState(encoder, StateFlags.WriteRgb | StateFlags.WriteZ | StateFlags.DepthTestLess | StateFlags.PtLines);
        
        // Submit(encoder, context.ViewId, material.Program, 1, 0);
        
        // encoder_end(encoder);
    }

    public override void Dispose()
    {
        base.Dispose();
        GC.SuppressFinalize(this);
        // DestroyVertexBuffer(ref _axisColoredVertexBuffer);
        // DestroyVertexBuffer(ref _colliderVertexBuffer);
    }
    
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Color color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly uint Color = color.ToAbgr();
        
        public static uint SizeInBytes => (uint)Unsafe.SizeOf<RenderingVertex>();
    }
}