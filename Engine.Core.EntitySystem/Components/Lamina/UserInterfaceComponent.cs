using System.Drawing;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Meshes.Builtins;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Extensions;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Silk.NET.Maths;

namespace Engine.Core.EntitySystem.Components.Lamina;

public partial class UserInterfaceComponent : ActorComponent, ILaminaRenderable, IDisposable
{
    [Component] public StaticMeshComponent MeshComponent;
    [Component] protected RootWidgetComponent RootWidget;
    
    public Vector2 InternalTextureSize { get; private set; } = new();

    /**
     * Public API
     */
    
    private bool _visible = true;
    public bool Visible
    {
        get => _visible;
        set
        {
            _visible = value;
            Dirty = true;
        }
    }
    public Vector2 Padding { get; set; } = new(0, 0);
    public Vector2 Size
    {
        get => _explicitSize ?? Backstage.Window.FramebufferSize;
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
    
    /**
     * End Public API
     */

    
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
        MeshComponent.Visible = false;
        RootWidget.IgnoreParentPosition();
        
        Backstage.Window.ResizeDebounced.Connect(this, _ =>
        {
            MainThreadTask.Run(() =>
            {
                Dirty = true;
                if (_layoutFunction != null)
                    SetLayout(_layoutFunction);
            });
        });
    }
    
    private Action<LaminaLayout>? _layoutFunction;
    public void SetLayout(Action<LaminaLayout> renderFunction)
    {
        _layoutFunction = renderFunction;
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
        if (_renderTargetReady && Math.Abs(InternalTextureSize.X - Size.X) < 1 &&
            Math.Abs(InternalTextureSize.Y - Size.Y) < 1)
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
        InternalTextureSize = sizeCreated;
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
        if (!Visible)
            return;
        renderContext.PushWidget(RootWidget);
        RootWidget.Transform.Position = (Padding.X, Padding.Y, 0);
        RootWidget.Transform.Scale = Transform.Scale - new Vector3(Padding.X * 2, Padding.Y * 2, 0);
        renderContext.ChildrenPosition = Padding;
        foreach (var child in GetChildren<WidgetComponent>())
        {
            child.PerformRender(renderContext);
        }
        renderContext.PopWidget();

        MeshComponent.Visible = true;
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
            builder
                .WithScissorRect()
                .WithWindingOrder(WindingOrder.None)
                .WithDepthTest(false, false)
                .WithAlphaBlending(false, false);
        });
        // AssetManager.Shared.Meshes.Put("Generated/InterfacePlaneMesh", this);
        return this;
    }
}