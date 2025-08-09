using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Communication.Signals;
using Engine.Core.Extensions;
using static Engine.Native.Bgfx.Bgfx;
using Transform = Engine.Core.Common.Transform;
// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

namespace Engine.Core.Assets.Meshes;

public enum WindingOrder
{
    Ccw = 0,
    Cw = 1
}

public class StaticMesh : IDisposable
{
    public bool IsValid { get; private set; }

    private WindingOrder WindingOrder { get; set; } = WindingOrder.Cw;

    protected VertexBuffer VertexBuffer = VertexBuffer.Invalid;
    protected IndexBuffer IndexBuffer = IndexBuffer.Invalid;
    protected VertexLayout Layout;

    public AssetVertex[] Vertices { get; private set; } = [];
    public ushort[] Indices { get; set; } = [];

    public Signal<AssetVertex[]> OnMeshLoaded { get; } = new();

    protected void LoadInternal(AssetVertex[] verts, ushort[] indices, WindingOrder windingOrder)
    {
        Vertices = verts;
        Indices = indices;

        WindingOrder = windingOrder;
        var renderVerts = new RenderingVertex[verts.Length];
        for (var i = 0; i < verts.Length; i++)
        {
            var v = verts[i];
            renderVerts[i] = new RenderingVertex(v.Position, v.TexCoord, v.VertexColor, Vector3.One);
        }

        Layout = CreateVertexLayout([
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.TexCoord0, 2, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true),
            new VertexLayoutAttribute(Attrib.Normal, 3, AttribType.Float, true, false)
        ]);
        VertexBuffer = CreateVertexBuffer(ref renderVerts, ref Layout);
        IndexBuffer = CreateIndexBuffer(ref indices);

        IsValid = true;

        OnMeshLoaded.Emit(verts);
    }

    public void PrepareRender(uint instanceCount, ref Transform[] worldTransforms, MaterialInstance[] materials, ref RenderContext context)
    {
        if (!IsValid)
        {
            context.InstanceTransformCount += instanceCount;
            return;
        }

        for (var i = 0; i < instanceCount; i++)
        {
            worldTransforms[i].ToFloatSpan(
                ref context.InstanceTransformPrepBuffer,
                (int)(context.InstanceTransformCount + i) * context.InstanceTransformStride
            );
            var startIndex = (int)(context.InstanceTransformCount + i) * context.InstanceTransformStride;
            context.InstanceTransformPrepBuffer[startIndex + 16] = materials[i].TintColor.X;
            context.InstanceTransformPrepBuffer[startIndex + 17] = materials[i].TintColor.Y;
            context.InstanceTransformPrepBuffer[startIndex + 18] = materials[i].TintColor.Z;
            context.InstanceTransformPrepBuffer[startIndex + 19] = materials[i].TintColor.W;
        }

        context.InstanceTransformCount += instanceCount;
    }

    public unsafe void Render(uint instanceCount, MaterialInstance material, ref RenderContext context, StateFlags extraFlags = StateFlags.None)
    {
        if (!IsValid || material == null || !material.Program.Valid || instanceCount == 0)
        {
            context.InstanceTransformCount += instanceCount;
            return;
        }

        var encoder = encoder_begin(false);
        SetVertexBuffer(encoder, VertexBuffer);
        SetIndexBuffer(encoder, IndexBuffer);
        
        SetInstanceDataBuffer(encoder, context.InstanceTransformBuffer, context.InstanceTransformCount, instanceCount);
        context.InstanceTransformCount += instanceCount;

        var stateFlags = StateFlags.WriteRgb | StateFlags.WriteA | StateFlags.WriteZ | StateFlags.DepthTestLess;
        if (WindingOrder == WindingOrder.Ccw)
            stateFlags |= StateFlags.CullCcw;
        else
            stateFlags |= StateFlags.CullCw;
        SetState(encoder, stateFlags | extraFlags);

        material.ApplyForRendering(encoder);
        Submit(encoder, context.ViewId, material.Program, 1, 0);

        encoder_end(encoder);
    }

    public virtual void Dispose()
    {
        Vertices = [];
        Indices = [];
        GC.SuppressFinalize(this);
        DestroyVertexBuffer(ref VertexBuffer);
        DestroyIndexBuffer(ref IndexBuffer);
        IsValid = false;
    }

    ~StaticMesh() => Dispose();

    public static StaticMesh CreateFromDisk(string path)
    {
        var filepath = Path.Combine("Assets", path);
        if (AssetManager.Shared(Assembly.GetCallingAssembly()).Meshes.TryGet(filepath, out var mesh))
            return mesh;

        ObjMeshLoader.LoadObj(filepath, out var vertices, out var indices);
        mesh = CreateFromMemoryWithoutCache(vertices, indices);
        AssetManager.Shared(Assembly.GetCallingAssembly()).Meshes.Put(filepath, mesh);
        return mesh;
    }

    public static StaticMesh CreateFromMemoryWithoutCache(AssetVertex[] verts, ushort[] indices, WindingOrder windingOrder = WindingOrder.Cw)
    {
        var newMesh = new StaticMesh();
        newMesh.LoadInternal(verts, indices, windingOrder);
        return newMesh;
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
