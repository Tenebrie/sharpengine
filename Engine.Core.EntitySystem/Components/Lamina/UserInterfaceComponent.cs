using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Meshes.Builtins;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Extensions;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using JetBrains.Annotations;
using Silk.NET.Maths;

namespace Engine.Core.EntitySystem.Components.Lamina;

[UsedImplicitly]
public partial class UserInterfaceComponent : ActorComponent, ILaminaRenderable, IDisposable
{
    [Component] public StaticMeshComponent MeshComponent;
    
    private Vector2 _inferredSize = new(1920, 1080);
    public Vector2 TextureSize { get; private set; } = new(1920, 1080);
    
    private Vector2D<int> FramebufferSize => Backstage.Window.GetScaledFramebufferSize();

    [OnReady]
    protected void OnReady()
    {
        _inferredSize = TextureSize = new Vector2(FramebufferSize.X, FramebufferSize.Y);
        MeshComponent.StaticMesh = InterfacePlaneMesh.Shared;
        MeshComponent.Material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").WithCache().Compile();
        MeshComponent.MaterialInstance = MeshComponent.Material.Instantiate();
        MeshComponent.SortOrder = 1;
        MeshComponent.CullingEnabled = false;
        Transform.Scale = new Vector3(TextureSize.X / FramebufferSize.X, TextureSize.Y / FramebufferSize.Y, 1) * 2;
        Dirty = true;
    }
    
    [Component] protected WidgetComponent RootWidget;
    public void SetLayout(Action<LaminaLayout> renderFunction)
    {
        var layout = new LaminaLayout(typeof(LaminaLayout));
        renderFunction(layout);
        RootWidget.Initialize(layout);
        Dirty = true;
    }

    public bool Dirty { get; set; }

    private int _activeRenderTargetIndex = -1;
    private struct RenderTargetData
    {
        public ITexture Target;
        public ITextureView RenderTargetView;
        public ITextureView ShaderResourceView;
    }
    private readonly List<RenderTargetData> _renderTargets = [];
    public ITextureView RenderTargetView => _renderTargets[_activeRenderTargetIndex].RenderTargetView;
    public ITextureView ShaderResourceView => _renderTargets[_activeRenderTargetIndex].ShaderResourceView;
    
    public void EnsureRenderTarget()
    {
        if (_activeRenderTargetIndex != -1 && _inferredSize.X <= TextureSize.X && _inferredSize.Y <= TextureSize.Y)
            return;
        
        foreach (var target in _renderTargets)
        {
            target.Target.Dispose();
            target.RenderTargetView.Dispose();
            target.ShaderResourceView.Dispose();
        }
        
        var targetTexture = RenderContext.Current.RenderDevice.CreateTexture(new TextureDesc
        {
            Name = "LaminaRT",
            Type = ResourceDimension.Tex2d,
            Width = (uint)TextureSize.X * 2,
            Height = (uint)TextureSize.Y * 2,
            Format = TextureFormat.RGBA8_UNorm_sRGB,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
        });
        var renderTargetView = targetTexture.GetDefaultView(TextureViewType.RenderTarget);
        var shaderResourceView = targetTexture.GetDefaultView(TextureViewType.ShaderResource);
        if (renderTargetView == null || shaderResourceView == null)
            throw new InvalidOperationException("Failed to create render target views for Lamina UI component");
        _renderTargets.Add(new RenderTargetData
        {
            Target = targetTexture,
            RenderTargetView = renderTargetView,
            ShaderResourceView = shaderResourceView
        });

        _activeRenderTargetIndex = 0;
        MeshComponent.MaterialInstance.SetRemoteTextureView(ShaderResourceView);
    }

    public void CollectCommandList(ILaminaRenderContext renderContext)
    {
        foreach (var child in GetChildren<WidgetComponent>())
        {
            child.PerformRender(renderContext);
        }
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var target in _renderTargets)
        {
            target.RenderTargetView.Dispose();
            target.ShaderResourceView.Dispose();
            target.Target.Dispose();
        }
    }
}

public class InterfacePlaneMesh : StaticMesh
{
    private static readonly InterfacePlaneMesh Instance = new();
    public static InterfacePlaneMesh Shared => Instance.Load();
    
    private bool _isLoaded = false;
    private InterfacePlaneMesh Load()
    {
        if (_isLoaded)
            return this;
        _isLoaded = true;

        var verts = TessellatedPlaneMesh.CreateVerticesXy();
        var indices = TessellatedPlaneMesh.CreateIndices();
        LoadInternal(verts, indices, WindingOrder.Ccw, builder =>
        {
            builder.WithDepthTest(false, false);
            builder.WithAlphaBlending(false, false);
        });
        // AssetManager.Shared.Meshes.Put("Generated/InterfacePlaneMesh", this);
        return this;
    }
}