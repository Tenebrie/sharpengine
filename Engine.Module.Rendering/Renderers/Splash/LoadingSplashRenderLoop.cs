using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;

namespace Engine.Module.Rendering.Renderers.Splash;

public class LoadingSplashRenderLoop(RenderingHost host)
{
    private static IDeviceContext ImmediateContext => RenderContext.Current.ImmediateContext;
    private static ISwapChain SwapChain => RenderContext.Current.SwapChain;

    private ITexture RenderTarget => host.RenderTarget;
    private ITextureView RenderTargetView => host.RenderTargetView;
    private ITextureView RenderDepthView => host.RenderDepthView;

    public void RenderEngineLoadingScreen()
    {
        host.CreateRenderTargets();
        
        ImmediateContext.ClearRenderTarget(RenderTargetView, new Vector4(0.0, 0.0, 0.0, 1.0), ResourceStateTransitionMode.Transition);
        ImmediateContext.ClearDepthStencil(RenderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);
        ImmediateContext.SetRenderTargets([RenderTargetView], RenderDepthView, ResourceStateTransitionMode.Transition);
        
        SplashRenderer.RenderOnce();

        var rtv = SwapChain.GetCurrentBackBufferRTV();
        var rtvTexture = rtv.GetTexture();
        
        ImmediateContext.ResolveTextureSubresource(
            RenderTarget,
            rtvTexture,
            new ResolveTextureSubresourceAttribs
            {
                Format = SwapChain.GetDesc().ColorBufferFormat,
                SrcTextureTransitionMode = ResourceStateTransitionMode.Transition,
                DstTextureTransitionMode = ResourceStateTransitionMode.Transition,
            }
        );
        
        SwapChain.Present(0);
    }
}