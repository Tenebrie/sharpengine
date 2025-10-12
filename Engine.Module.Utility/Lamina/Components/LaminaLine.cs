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
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[UsedImplicitly]
public partial class LaminaLine : WidgetComponent
{
    private readonly List<Vector2> _points = [];
    private LaminaLineMesh? _mesh = null;
    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

    protected override void PopulateIntrinsics(LaminaLayout layout)
    {
        if (layout is not LineLayout lineLayout)
            throw new ArgumentException($"Expected layout of type {nameof(LineLayout)}, got {layout.GetType().Name}");

        var points = lineLayout.Props.Points;
        var minValues = new Vector2(points.Select(p => p.X).Min(), points.Select(p => p.Y).Min());
        var maxValues = new Vector2(points.Select(p => p.X).Max(), points.Select(p => p.Y).Max());
        Size = maxValues - minValues;
    }

    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not LineLayout lineLayout)
            throw new ArgumentException($"Expected layout of type {nameof(LineLayout)}, got {layout.GetType().Name}");

        RegenerateMesh(lineLayout.Props);
        if (_mesh == null)
            return;
        
        if (_material == null || _materialInstance == null)
        {
            _material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
            _materialInstance = _material.Instantiate().SetTintColor(lineLayout.Props.Color);
        }
        
        Position = context.Position;
            
        Transform = Transform.Identity;
        Transform.Position = context.Position.ToVector3();
        
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [Transform.Snapshot()],
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
            _points.Add(new Vector2(point.X, point.Y));
        }
        var minValues = new Vector2(_points.Select(p => p.X).Min(), _points.Select(p => p.Y).Min());
        var maxValues = new Vector2(_points.Select(p => p.X).Max(), _points.Select(p => p.Y).Max());
        Size = maxValues - minValues;

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