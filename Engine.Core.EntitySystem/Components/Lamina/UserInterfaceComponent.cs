using System.Drawing;
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
    [Component] protected WidgetComponent RootWidget;

    public Vector2 TextureSize { get; private set; } = new();
    
    public Vector2 Padding { get; set; } = new(0, 0);
    public Vector2 Size
    {
        get => _explicitSize ?? new Vector2(FramebufferSize.X, FramebufferSize.Y);
        set => _explicitSize = value;
    }
    public Vector2 ContentSize => Size - Padding * 2;
    
    private Vector2? _explicitSize = null;


    private Color _backgroundColor = Color.FromArgb(0, 0, 0, 0);
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value.A == 0 ? Color.FromArgb(0, 0, 0, 0) : value;
            Dirty = true;
        }
    }

    
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
        Transform.Scale = new Vector3(Size.X, Size.Y, 1);
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

    private struct RenderTargetData
    {
        public ITexture Target;
        public ITextureView RenderTargetView;
        public ITextureView ShaderResourceView;
    }

    private bool _renderTargetReady = false;
    private RenderTargetData _renderTargets = default;
    public ITextureView RenderTargetView => _renderTargets.RenderTargetView;
    public ITextureView ShaderResourceView => _renderTargets.ShaderResourceView;
    
    public void EnsureRenderTarget()
    {
        if (_renderTargetReady && Math.Abs(TextureSize.X - Size.X) < 1 &&
            Math.Abs(TextureSize.Y - Size.Y) < 1)
        {
            return;
        }
        
        
        if (_renderTargetReady)
            _renderTargets.Target.Dispose();

        var sizeCreated = Size;
        var targetTexture = RenderContext.Current.RenderDevice.CreateTexture(new TextureDesc
        {
            Type = ResourceDimension.Tex2d,
            Width = (uint)sizeCreated.X * 2,
            Height = (uint)sizeCreated.Y * 2,
            Format = TextureFormat.RGBA8_UNorm_sRGB,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
        });
        var renderTargetView = targetTexture.GetDefaultView(TextureViewType.RenderTarget);
        var shaderResourceView = targetTexture.GetDefaultView(TextureViewType.ShaderResource);
        if (renderTargetView == null || shaderResourceView == null)
            throw new InvalidOperationException("Failed to create render target views for Lamina UI component");

        Transform.Scale = new Vector3(sizeCreated.X, sizeCreated.Y, 1);
        TextureSize = sizeCreated;
        _renderTargets = new RenderTargetData
        {
            Target = targetTexture,
            RenderTargetView = renderTargetView,
            ShaderResourceView = shaderResourceView
        };

        _renderTargetReady = true;
        MeshComponent.Material.InvalidateCache();
        MeshComponent.Material.SetRemoteTextureView(ShaderResourceView);
    }

    public void CollectCommandList(ILaminaRenderContext renderContext)
    {
        renderContext.Position += Padding;
        foreach (var child in GetChildren<WidgetComponent>())
        {
            child.PerformRender(renderContext);
        }
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
        var renderingModule = Backstage.RenderingModule;
        renderingModule?.UnregisterLamina(Rid);
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _renderTargets.Target.Dispose();
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