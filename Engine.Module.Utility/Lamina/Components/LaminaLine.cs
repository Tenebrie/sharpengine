using System.Drawing;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[UsedImplicitly]
public partial class LaminaLine : WidgetComponent
{
    private List<Vector2> _points = [];
    private LaminaLineMesh? _mesh = null;
    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

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
        // TODO: Set size from bounding box of points
        Size = new Vector2(120, 64);
            
        Transform = Transform.Identity;
        var screenPosition = context.Position / RenderContext.Current.RenderTargetSize - Vector2.One / 2;
        var scale = Size / RenderContext.Current.RenderTargetSize;
        Transform.Scale = new Vector3(scale.X, scale.Y, 1.0) * 2;
        Transform.Position = new Vector3(screenPosition.X, -screenPosition.Y, 0) * 2 + new Vector3(scale.X, -scale.Y, 0.0);
        
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [Transform],
            Material = _material,
            Mesh = _mesh,
            RenderScript = IRenderScript.Default,
            MaterialInstances = [_materialInstance]
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
            var adjusted = point / RenderContext.Current.RenderTargetSize;
            _points.Add(new Vector2(adjusted.X, -adjusted.Y));
        }

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
                _verts = new AssetVertex[points.Count];
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
                _indices = new uint[(points.Count - 1) * 2];
            for (var i = 0; i < points.Count - 1; i++)
            {
                _indices[i * 2] = (uint)i;
                _indices[i * 2 + 1] = (uint)(i + 1);
            }

            LoadCustomized(_verts, _indices, WindingOrder.Ccw, Usage.Default, builder =>
            {
                builder.WithPrimitiveTopology(PrimitiveTopology.LineList);
                builder.WithDepthTest(false, false);
                builder.WithAlphaBlending(false, false);
            });
        }
    }
}