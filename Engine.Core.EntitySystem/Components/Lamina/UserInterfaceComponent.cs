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
    private Vector2 _textureSize = new(1920, 1080);
    
    private Vector2D<int> FramebufferSize => Backstage.Window.GetScaledFramebufferSize();

    [OnReady]
    protected void OnReady()
    {
        MeshComponent.StaticMesh = InterfacePlaneMesh.Shared;
        MeshComponent.Material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").WithCache().Compile();
        MeshComponent.MaterialInstance = MeshComponent.Material.Instantiate();
        Transform.Scale = new Vector3(_textureSize.X / FramebufferSize.X, _textureSize.Y / FramebufferSize.Y, 1) * 2;
        Dirty = true;
    }
    
    [Component] protected WidgetComponent RootWidget;
    public void SetLayout(Action<LaminaLayout> renderFunction)
    {
        var layout = new LaminaLayout(typeof(LaminaLayout));
        renderFunction(layout);
        RootWidget.SetLayout(layout);
        Dirty = true;
    }

    private readonly Transform[] _singleComponentTransforms = new Transform[1];
    private RenderRequest? _renderRequest;
    
    public bool Dirty { get; set; }
    
    private ITexture? _renderTarget;
    public ITexture RenderTarget => _renderTarget!;
    public ITextureView RenderTargetView { get; private set; } = null!;
    public ITextureView ShaderResourceView { get; private set; } = null!;
    
    public void EnsureRenderTarget()
    {
        if (_renderTarget is not null && _inferredSize.X <= _textureSize.X && _inferredSize.Y <= _textureSize.Y)
            return;
        
        _renderTarget = RenderContext.Current.RenderDevice.CreateTexture(new TextureDesc
        {
            Name = "LaminaRT",
            Type = ResourceDimension.Tex2d,
            Width = (uint)_textureSize.X * 2,
            Height = (uint)_textureSize.Y * 2,
            Format = TextureFormat.RGBA8_UNorm_sRGB,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
        });
        RenderTargetView = _renderTarget.GetDefaultView(TextureViewType.RenderTarget);
        ShaderResourceView = _renderTarget.GetDefaultView(TextureViewType.ShaderResource);
        if (RenderTargetView == null || ShaderResourceView == null)
            throw new InvalidOperationException("Failed to create render target views for Lamina UI component");
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
        ShaderResourceView?.Dispose();
        RenderTargetView?.Dispose();
        _renderTarget?.Dispose();
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
        LoadInternal(verts, indices, WindingOrder.Ccw);
        // AssetManager.Shared.Meshes.Put("Generated/InterfacePlaneMesh", this);
        return this;
    }
}