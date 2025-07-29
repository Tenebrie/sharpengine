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

public class BoundingSphere
{
    public bool IsValid { get; private set; } = false;
    private Vector3 Position { get; set; }
    private double Radius { get; set; }
    
    public void Load(AssetVertex[] verts)
    {
        if (verts.Length < 3)
        {
            // Not enough vertices to form a sphere
            Position = Vector3.Zero;
            Radius = 0.01f;
            return;
        }

        try
        {
            CalculateRittersBoundingSphere(verts);
        } catch (Exception ex)
        {
            Logger.Error("Failed to calculate bounding sphere: " + ex.Message);
            Position = Vector3.Zero;
            Radius = 0.01f;
            return;
        }

        // Logger.InfoF("Bounding sphere calculated: Position = {0}, Radius = {1}", Position, Radius);
        PrepareRendering();
    }

    private void CalculateRittersBoundingSphere(AssetVertex[] verts)
    {
        var p0 = verts[0].Position;
        var p1 = verts.OrderByDescending(v => v.Position.DistanceSquaredTo(p0)).First().Position;
        var p2 = verts.OrderByDescending(v => v.Position.DistanceSquaredTo(p1)).First().Position;
        var center = (p1 + p2) * 0.5;
        var radius = p1.DistanceTo(p2) * 0.5;

        foreach (var v in verts)
        {
            var d = v.Position.DistanceTo(center);
            if (!(d > radius))
                continue;
            
            var newRadius = (radius + d) * 0.5;
            var direction = (v.Position - center) / d;
            center += direction * (newRadius - radius);
            radius = newRadius;
        }
        
        Position = center;
        Radius   = radius;
        IsValid  = true;
    }
    
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private VertexLayout _layout;

    private void PrepareRendering()
    {
        List<RenderingVertex> verts = [];
        List<int> indices = [];

        const int segmentCount = 32;
        for (var i = 0; i < segmentCount; i++)
        {
            var angle = i * Math.PI * 2 / segmentCount;
            var x = (float)Math.Cos(angle);
            var y = (float)Math.Sin(angle);
            verts.Add(new RenderingVertex(new Vector3(x, 0, y), Color.LightGreen));
            verts.Add(new RenderingVertex(new Vector3(x, y, 0), Color.DeepSkyBlue));
            verts.Add(new RenderingVertex(new Vector3(0, x, y), Color.LightCoral));

            var next = (i + 1) % segmentCount;

            indices.Add((ushort)(3 * i + 0));
            indices.Add((ushort)(3 * next + 0));
            indices.Add((ushort)(3 * i + 1));
            indices.Add((ushort)(3 * next + 1));
            indices.Add((ushort)(3 * i + 2));
            indices.Add((ushort)(3 * next + 2));
        }
        
        var vertsArray = verts.ToArray();
        var indicesArray = new ushort[indices.Count];
        for (var i = 0; i < indices.Count; i++)
            indicesArray[i] = (ushort)indices[i];
        
        CreateVertexLayout(ref _layout, [
            new VertexLayoutAttribute(Attrib.Position, 3, AttribType.Float, true, false),
            new VertexLayoutAttribute(Attrib.Color0, 4, AttribType.Uint8, true, true)
        ]);
        _vertexBuffer = CreateVertexBuffer(ref vertsArray, ref _layout);
        _indexBuffer = CreateIndexBuffer(ref indicesArray);
        IsValid = true;
    }

    public unsafe void Render(uint instanceCount, ref Transform[] worldTransforms, ushort viewId, Material material)
    {
        if (!IsValid)
        {
            Logger.Error("BoundingSphere is not initialized. Call Load() first.");
            return;
        }
        var idb = new InstanceDataBuffer();
        const ushort bytesPerMatrix = 16 * sizeof(float);
        alloc_instance_data_buffer(&idb, instanceCount, bytesPerMatrix);
        
        var instanceDataArray = (float*)idb.data;
        Span<float> mat = stackalloc float[16];
        for (var i = 0; i < instanceCount; i++)
        {
            var t = Transform.Copy(worldTransforms[i]);
            t.TranslateLocal(Position);
            t.Rotation = Quat.Identity;
            t.Rescale(Radius);
            t.ToFloatSpan(ref mat);
            for (var j = 0; j < 16; j++)
            {
                instanceDataArray[i * 16 + j] = mat[j];
            }
        }
        SetInstanceDataBuffer(&idb, 0, instanceCount);
        
        SetVertexBuffer(_vertexBuffer);
        SetIndexBuffer(_indexBuffer);
        SetState(StateFlags.WriteRgb | StateFlags.WriteZ | StateFlags.DepthTestLess | StateFlags.PtLines);
        
        submit(viewId, material.Program, 1, 0);
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Color color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly uint Color = color.ToAbgr();
    }
}