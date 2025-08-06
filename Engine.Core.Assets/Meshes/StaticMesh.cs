using System.Drawing;
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

    private void LoadInternal(AssetVertex[] verts, ushort[] indices, WindingOrder windingOrder)
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

    public void PrepareRender(uint instanceCount, ref Transform[] worldTransforms, ref RenderContext context)
    {
        if (!IsValid)
        {
            context.InstanceTransformCount += instanceCount;
            return;
        }
        
        for (var i = 0; i < instanceCount; i++)
            worldTransforms[i].ToFloatSpan(
                ref context.InstanceTransformPrepBuffer,
                (int)(context.InstanceTransformCount + i) * context.InstanceTransformStride
            );
        
        context.InstanceTransformCount += instanceCount;
    }

    public unsafe void Render(uint instanceCount, MaterialInstance material, ref RenderContext context, StateFlags extraFlags = StateFlags.None)
    {
        if (!IsValid || material == null || !material.Program.Valid)
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
        
        material.BindTexture(encoder);
        Submit(encoder, context.ViewId, material.Program, 1, 0);
        
        encoder_end(encoder);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (VertexBuffer.Valid)
            DestroyVertexBuffer(ref VertexBuffer);
        if (IndexBuffer.Valid)
            DestroyIndexBuffer(ref IndexBuffer);
        IsValid = false;
    }
    
    protected static string ComputeMeshHash(AssetVertex[] vertices, ushort[] indices)
    {
        // Create a more efficient hash by directly hashing the binary data
        var vertexBytes = new byte[vertices.Length * sizeof(double) * 5 + vertices.Length * 4]; // position(3 doubles) + texcoord(2 doubles) + color(4 bytes)
        var indexBytes = new byte[indices.Length * sizeof(ushort)];

        var vertexSpan = new Span<byte>(vertexBytes);
        var indexSpan = new Span<byte>(indexBytes);

        // Copy vertex data
        for (var i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];
            var offset = i * (sizeof(double) * 5 + 4);

            // Position (3 doubles)
            BitConverter.TryWriteBytes(vertexSpan[offset..], vertex.Position.X);
            BitConverter.TryWriteBytes(vertexSpan[(offset + 8)..], vertex.Position.Y);
            BitConverter.TryWriteBytes(vertexSpan[(offset + 16)..], vertex.Position.Z);

            // TexCoord (2 doubles)
            BitConverter.TryWriteBytes(vertexSpan[(offset + 24)..], vertex.TexCoord.X);
            BitConverter.TryWriteBytes(vertexSpan[(offset + 32)..], vertex.TexCoord.Y);

            // Color (4 bytes)
            vertexSpan[offset + 40] = vertex.VertexColor.R;
            vertexSpan[offset + 41] = vertex.VertexColor.G;
            vertexSpan[offset + 42] = vertex.VertexColor.B;
            vertexSpan[offset + 43] = vertex.VertexColor.A;
        }

        // Copy index data
        for (var i = 0; i < indices.Length; i++)
        {
            BitConverter.TryWriteBytes(indexSpan[(i * sizeof(ushort))..], indices[i]);
        }

        // Combine vertex and index data for final hash
        var combinedBytes = new byte[vertexBytes.Length + indexBytes.Length];
        vertexBytes.CopyTo(combinedBytes, 0);
        indexBytes.CopyTo(combinedBytes, vertexBytes.Length);

        var hashBytes = SHA256.HashData(combinedBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public static StaticMesh CreateFromMemory(AssetVertex[] verts, ushort[] indices, WindingOrder windingOrder = WindingOrder.Cw)
    {
        var hash = ComputeMeshHash(verts, indices);

        if (AssetManager.Meshes.TryGet(hash, out var mesh))
            return mesh;

        var newMesh = new StaticMesh();
        newMesh.LoadInternal(verts, indices, windingOrder);
        AssetManager.PutMesh(hash, newMesh);
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