using System.Drawing;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaLine : LaminaWidgetComponent<LineLayout>
{
    private readonly List<Vector2> _points = [];
    private LaminaLineMesh? _mesh = null;
    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

    public override void OnPopulateIntrinsics(LineLayout layout)
    {
        var points = layout.Props.Points;
        var minValues = new Vector2(points.Select(p => p.X).Min(), points.Select(p => p.Y).Min());
        var maxValues = new Vector2(points.Select(p => p.X).Max(), points.Select(p => p.Y).Max());
        // Size = maxValues - minValues;
    }

    public override void OnRender(LineLayout layout, ILaminaRenderContext context)
    {
        RegenerateMesh(layout.Props);
        if (_mesh == null)
            return;
        
        if (_material == null || _materialInstance == null)
        {
            _material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
            _materialInstance = _material.Instantiate().SetTintColor(layout.Props.Color);
        }
        
        Transform = Transform.Identity;
        Transform.Position = (Math.Round(context.OffsetToParent.X), Math.Round(context.OffsetToParent.Y), 0.0);
        
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [WorldTransformNoScale.Snapshot()],
            Material = _material,
            Mesh = _mesh,
            RenderScript = IRenderScript.Default,
            MaterialInstances = [_materialInstance.Snapshot()]
        });
    }
    
    private void RegenerateMesh(LaminaLineProps props)
    {
        if (_mesh != null && props.Points.Count == _points.Count && props.Points.SequenceEqual(_points))
            return;
        
        if (props.Points.Count < 2)
            return;
        
        _points.Clear();
        foreach (var point in props.Points)
        {
            // TODO: Where is the 0.25 coming from?
            _points.Add(new Vector2(Math.Round(point.X), Math.Round(point.Y)) + (0.25, 0.25));
        }
        var minValues = new Vector2(_points.Select(p => p.X).Min(), _points.Select(p => p.Y).Min());
        var maxValues = new Vector2(_points.Select(p => p.X).Max(), _points.Select(p => p.Y).Max());
        // Size = maxValues - minValues;

        _mesh ??= new LaminaLineMesh();
        _mesh.Generate(_points);
    }
    
    internal class LaminaLineMesh : StaticMesh
    {
        private AssetVertex[] _verts = [];
        private uint[] _indices = [];
        internal void Generate(List<Vector2> points)
        {
            if (_verts.Length != points.Count)
                _verts = new AssetVertex[points.Count * 2];
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                _verts[i] = new AssetVertex
                {
                    Position = point.ToVector3(),
                    VertexColor = Color.White
                };
            }

            if (_indices.Length != (points.Count - 1) * 2)
                _indices = new uint[(points.Count - 1) * 4];
            for (var i = 0; i < points.Count - 1; i++)
            {
                _indices[i * 2] = (uint)i;
                _indices[i * 2 + 1] = (uint)(i + 1);
            }
            
            // Pass 2
            // for (var i = 0; i < points.Count; i++)
            // {
            //     var point = points[i];
            //     _verts[points.Count + i] = new AssetVertex
            //     {
            //         Position = point.ToVector3() + new Vector3(1,1,0),
            //         VertexColor = Color.White
            //     };
            // }
            //
            // for (var i = 0; i < points.Count - 1; i++)
            // {
            //     _indices[(points.Count - 1) * 2 + i * 2] = (uint)points.Count + (uint)i;
            //     _indices[(points.Count - 1) * 2 + i * 2 + 1] = (uint)points.Count + (uint)(i + 1);
            // }

            LoadCustomized(_verts, _indices, WindingOrder.Ccw, Usage.Default, builder =>
            {
                builder.WithPrimitiveTopology(PrimitiveTopology.LineList);
                builder.WithDepthTest(false, false);
                builder.WithAlphaBlending(false, false);
            });
        }
    }
}
