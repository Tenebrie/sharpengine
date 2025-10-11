using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Meshes.Builtins;
using Engine.Core.Assets.Rendering;
using Engine.Core.Attributes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Extensions;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Engine.Core.Modules;
using JetBrains.Annotations;
using Silk.NET.Maths;

namespace Engine.Core.EntitySystem.Components.Lamina;

[UsedImplicitly]
public partial class UserInterfaceComponent : ActorComponent, ILaminaRenderable, IDisposable
{
    [Component] public StaticMeshComponent MeshComponent;
    [Component] protected WidgetComponent RootWidget;

    public Vector2? Size { get; set; } = null;
    public Vector2 RenderedSize => Size ?? new Vector2(FramebufferSize.X, FramebufferSize.Y);
    public Vector2 TextureSize { get; private set; } = new();
    
    private Vector2D<int> FramebufferSize => Backstage.Window.GetScaledFramebufferSize();

    [OnReady]
    protected void OnReady()
    {
        MeshComponent.StaticMesh = InterfacePlaneMesh.Shared;
        MeshComponent.Material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
        MeshComponent.MaterialInstance = MeshComponent.Material.Instantiate();
        MeshComponent.SortOrder = 1;
        MeshComponent.CullingEnabled = false;
        Transform.Position = new Vector3(0, 0, 0);
        Transform.Scale = new Vector3(RenderedSize.X, RenderedSize.Y, 1);
        Dirty = true;
    }
    
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
        if (_activeRenderTargetIndex != -1 && Math.Abs(TextureSize.X - RenderedSize.X) < 1 && Math.Abs(TextureSize.Y - RenderedSize.Y) < 1)
            return;
        
        Logger.Info(RenderedSize);
        foreach (var target in _renderTargets)
            target.Target.Dispose();
        
        var targetTexture = RenderContext.Current.RenderDevice.CreateTexture(new TextureDesc
        {
            Type = ResourceDimension.Tex2d,
            Width = (uint)RenderedSize.X * 2,
            Height = (uint)RenderedSize.Y * 2,
            Format = TextureFormat.RGBA8_UNorm_sRGB,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
        });
        var renderTargetView = targetTexture.GetDefaultView(TextureViewType.RenderTarget);
        var shaderResourceView = targetTexture.GetDefaultView(TextureViewType.ShaderResource);
        if (renderTargetView == null || shaderResourceView == null)
            throw new InvalidOperationException("Failed to create render target views for Lamina UI component");

        Transform.Scale = new Vector3(RenderedSize.X, RenderedSize.Y, 1);
        TextureSize = RenderedSize;
        _renderTargets.Add(new RenderTargetData
        {
            Target = targetTexture,
            RenderTargetView = renderTargetView,
            ShaderResourceView = shaderResourceView
        });

        _activeRenderTargetIndex = 0;
        MeshComponent.Material.SetRemoteTextureView(ShaderResourceView);
    }

    public void CollectCommandList(ILaminaRenderContext renderContext)
    {
        foreach (var child in GetChildren<WidgetComponent>())
        {
            child.PerformRender(renderContext);
        }
    }
    
    public long Rid = -1;
    
    [OnCreate]
    [OnModuleReload(EngineModule.Rendering)]
    protected void OnRegisterOnRenderingServer()
    { 
        var renderingModule = Backstage.RenderingModule;
        if (renderingModule == null)
            return; 
        Rid = renderingModule.Register(this);
    }
    
    [OnUpdate]
    protected void OnReregisterOnRenderingServer()
    { 
        var renderingModule = Backstage.RenderingModule;
        renderingModule?.UpdateRegistered(Rid, this);
    }
    
    [OnDestroy]
    protected void OnUnregisterOnRenderingServer()
    {
        if (Rid == -1)
            return;
        var renderingModule = Backstage.RenderingModule;
        renderingModule?.UnregisterLamina(Rid);
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
        LoadCustomized(verts, indices, WindingOrder.Ccw, Usage.Immutable, builder =>
        {
            builder.WithWindingOrder(WindingOrder.None);
            builder.WithDepthTest(false, false);
            builder.WithAlphaBlending(false, false);
        });
        // AssetManager.Shared.Meshes.Put("Generated/InterfacePlaneMesh", this);
        return this;
    }
}