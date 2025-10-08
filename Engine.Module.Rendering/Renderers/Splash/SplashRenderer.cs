using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Extensions;
using ValueType = Diligent.ValueType;

namespace Engine.Module.Rendering.Renderers.Splash;

public static class SplashRenderer
{
    public static unsafe void RenderOnce()
    {
        var material = MaterialBuilder
            .CreateFromDisk("Shaders/UserInterface/Text")
            .SetTexture("Textures/splash.png")
            .Compile();
        
        var meshPipeline = PipelineBuilder.PrepareMesh()
            // Position
            .WithLayoutElement(new LayoutElement
            {
                InputIndex = 0,
                NumComponents = 3,
                ValueType = ValueType.Float32,
                IsNormalized = false,
            })
            // UV
            .WithLayoutElement(new LayoutElement
            {
                InputIndex = 1,
                NumComponents = 2,
                ValueType = ValueType.Float32,
                IsNormalized = false,
            })
            // Color
            .WithLayoutElement(new LayoutElement
            {
                InputIndex = 2,
                NumComponents = 4,
                ValueType = ValueType.Float32,
                IsNormalized = false,
            })
            .WithDepthTest(false, false)
            .WithAlphaBlending(true, true)
            .Build();
        
        var vertexBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "Splash Vertex Buffer",
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.VertexBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            Size = RenderingVertex.SizeInBytes * 4
        });
        
        var indices = new ushort[] { 0, 1, 2, 1, 3, 2 };

        BufferData indexData;
        fixed (ushort* indicesPtr = indices)
        {
            indexData = new BufferData
                { Data = (IntPtr)indicesPtr, DataSize = (uint)(sizeof(ushort) * indices.Length) };
        }
        var indexBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "Splash Index Buffer",
            Usage = Usage.Immutable,
            BindFlags = BindFlags.IndexBuffer,
            CPUAccessFlags = CpuAccessFlags.None,
            Size = (ulong)(Unsafe.SizeOf<ushort>() * 6)
        }, indexData);
        
        var context = RenderContext.Current;
        
        var pso = AssetManager.Shared.Pipelines.Produce(meshPipeline, material.Pipeline);
        context.ImmediateContext.SetPipelineState(pso);
        var screenSize = new Vector2(context.SwapChain.GetDesc().Width, context.SwapChain.GetDesc().Height);
        var desiredSize = new Vector3(512, 512, 0);
        var sizeMod = new Vector3(desiredSize.X / screenSize.X, desiredSize.Y / screenSize.Y, 1.0f);
        
        var vertexBufferSpan = context.ImmediateContext.MapBuffer<RenderingVertex>(vertexBuffer, MapType.Write, MapFlags.Discard);
        vertexBufferSpan[0] = new RenderingVertex(new Vector3(-1, 1, 0) * sizeMod, new Vector2(0f, 0f), Color.White);
        vertexBufferSpan[1] = new RenderingVertex(new Vector3(1, 1, 0) * sizeMod, new Vector2(1f, 0f), Color.White);
        vertexBufferSpan[2] = new RenderingVertex(new Vector3(-1, -1, 0) * sizeMod, new Vector2(0f, 1f), Color.White);
        vertexBufferSpan[3] = new RenderingVertex(new Vector3(1, -1, 0) * sizeMod, new Vector2(1f, 1f), Color.White);
        context.ImmediateContext.UnmapBuffer(vertexBuffer, MapType.Write);
        context.ImmediateContext.SetVertexBuffers(0, [vertexBuffer], [0ul], ResourceStateTransitionMode.Transition);
        context.ImmediateContext.SetIndexBuffer(indexBuffer, 0, ResourceStateTransitionMode.Transition);
        var srb = material.BindMaterial(pso);
        context.ImmediateContext.CommitShaderResources(srb, ResourceStateTransitionMode.Transition);

        context.ImmediateContext.DrawIndexed(new DrawIndexedAttribs
        {
            NumIndices = 6,
            IndexType = ValueType.UInt16,
        });
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct RenderingVertex(Vector3 position, Vector2 uv, Color color)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly Vector2Float Uv = uv.Downgrade();
        public readonly Vector4Float Color = color.ToVector4().Downgrade();
        
        public static uint SizeInBytes => (uint)Unsafe.SizeOf<RenderingVertex>();
    }
}
