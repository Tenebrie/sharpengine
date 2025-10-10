using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Filesystem;
using Engine.Core.Logging;
using Engine.Core.Modules.EntitySystem;
using Engine.Core.Profiling.Attributes;
using Engine.Module.Rendering.Utilities;

namespace Engine.Module.Rendering.Computers;

public class CullingComputer : IDisposable
{
    private readonly IPipelineState _pipelineState;
    private readonly IShaderResourceBinding _srb;
    private readonly IShaderResourceVariable _constantsVariable;
    private readonly IShaderResourceVariable _inDataVariable;
    private readonly IShaderResourceVariable _outDataVariable;
    
    private readonly IBuffer _constantBuffer;
    private readonly InfiniteInstanceWriteOnlyBuffer<InstanceEntry> _instanceEntryBuffer;
    private readonly InfiniteInstanceReadOnlyBuffer<float> _instanceOutputBuffer;

    private bool _deviceBusy = false;
    private readonly IFence _fence;
    private ulong _fenceValue = 0;

    public CullingComputer(RenderingHost host)
    {
        var computeShader = RenderContext.Current.RenderDevice.CreateShader(new ShaderCreateInfo
        {
            FilePath = FileResolver.Resolve("Assets/Shaders/Compute/Culling.comp.hlsl"),
            ShaderSourceStreamFactory = RenderContext.Current.ShaderFactory,
            Desc = new ShaderDesc
            {
                ShaderType = ShaderType.Compute
            },
            SourceLanguage = ShaderSourceLanguage.Hlsl
        }, out _);
        
        _pipelineState = RenderContext.Current.RenderDevice.CreateComputePipelineState(new ComputePipelineStateCreateInfo
        {
            PSODesc = new PipelineStateDesc
            {
                PipelineType = PipelineType.Compute,
                ResourceLayout = new PipelineResourceLayoutDesc
                {
                    Variables =
                    [
                        new ShaderResourceVariableDesc
                        {
                            ShaderStages = ShaderType.Compute,
                            Name = "Constants",
                            Type = ShaderResourceVariableType.Dynamic,
                        },
                        new ShaderResourceVariableDesc
                        {
                            ShaderStages = ShaderType.Compute,
                            Name = "InData",
                            Type = ShaderResourceVariableType.Dynamic
                        },
                        new ShaderResourceVariableDesc
                        {
                            ShaderStages = ShaderType.Compute,
                            Name = "OutData",
                            Type = ShaderResourceVariableType.Dynamic
                        }
                    ],
                }
            },
            Cs = computeShader
        });
        
        _srb = _pipelineState.CreateShaderResourceBinding(true);
        _constantsVariable = _srb.GetVariableByName(ShaderType.Compute, "Constants");
        _inDataVariable = _srb.GetVariableByName(ShaderType.Compute, "InData");
        _outDataVariable = _srb.GetVariableByName(ShaderType.Compute, "OutData");
        
        _constantBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "CullingComputer Constant Buffer",
            Size = ConstantParams.SizeInBytes,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.UniformBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
        });
        _pipelineState.GetStaticVariableByName(ShaderType.Compute, "Constants")?.Set(_constantBuffer, SetShaderResourceFlags.None);
        
        _fence = RenderContext.Current.RenderDevice.CreateFence(new FenceDesc { Name = "CullingComputer Fence" });
        
        _instanceEntryBuffer = new InfiniteInstanceWriteOnlyBuffer<InstanceEntry>();
        _instanceOutputBuffer = new InfiniteInstanceReadOnlyBuffer<float>();
        _instanceEntryBuffer.FrameStart();
    }

    private int _activeBufferIndex = 0;
    private readonly List<List<ICullable>> _submitQueues = [[], []];
    private List<ICullable> BackQueue => _submitQueues[1 - _activeBufferIndex];
    private List<ICullable> FrontQueue => _submitQueues[_activeBufferIndex];
    
    private readonly Dictionary<ICullable, bool> _currentResults = new();

    private List<float> _valuesBuffer = [];
    public void ReadResultsAndPrepare()
    {
        _deviceBusy = _fence.GetCompletedValue() < _fenceValue;
        if (_deviceBusy)
            return;
        
        _instanceEntryBuffer.FrameStart();
        _currentResults.Clear();
        if (BackQueue.Count == 0)
            return;

        _instanceOutputBuffer.Read(BackQueue.Count, ref _valuesBuffer);

        for (var i = 0; i < BackQueue.Count; i++)
        {
            var visible = _valuesBuffer[i] >= 0.5f;
            var t = BackQueue[i];
            _currentResults[t] = visible;
        }

        FrontQueue.Clear();
        BackQueue.Clear();
    }
    
    public void QueueForCulling(ICullable renderable)
    {
        if (_deviceBusy)
            return;
        FrontQueue.Add(renderable);
    }
    
    public bool IsVisible(ICullable renderable)
    {
        if (_currentResults.TryGetValue(renderable, out var visible))
            return visible;
        return false;
    }

    private InstanceEntry[] _tempEntries = [];
    public void SubmitCurrentQueue(ICamera.Plane[] frustumPlanes)
    {
        if (FrontQueue.Count == 0 || _deviceBusy)
            return;
        
        // Entry buffer
        if (_tempEntries.Length < FrontQueue.Count)
            Array.Resize(ref _tempEntries, FrontQueue.Count);
        for (var index = 0; index < FrontQueue.Count; index++)
        {
            var renderable = FrontQueue[index];
            if (renderable is Atom atom && !Atom.IsValid(atom))
                continue;
            _tempEntries[index] = new InstanceEntry(
                renderable.BoundingSphereWorldOrigin, renderable.BoundingSphereWorldRadius);
        }

        var inputBufferTickets = _instanceEntryBuffer.Write(FrontQueue.Count, _tempEntries);
        var outputBufferTickets = _instanceOutputBuffer.GetBindTickets(FrontQueue.Count);
        if (inputBufferTickets.Count != outputBufferTickets.Count)
            throw new Exception("Page count mismatch between input and output buffers: " +
                                $"{inputBufferTickets.Count} vs {outputBufferTickets.Count}");
        var pageCount = inputBufferTickets.Count;
        
        // Constant buffer
        const uint threadsPerGroup = 256;

        for (var i = 0; i < pageCount; i++)
        {
            var entitiesThisPage = inputBufferTickets[i].Count;
            var constantData = new ConstantParams(entitiesThisPage, frustumPlanes);
        
            var span = RenderContext.Current.ImmediateContext.MapBuffer<byte>(_constantBuffer, MapType.Write, MapFlags.Discard);
            MemoryMarshal.Write(span, in constantData);
            RenderContext.Current.ImmediateContext.UnmapBuffer(_constantBuffer, MapType.Write);
            
            RenderContext.Current.ImmediateContext.SetPipelineState(_pipelineState);
        
            _constantsVariable.Set(_constantBuffer, SetShaderResourceFlags.None);
            _inDataVariable.Set(inputBufferTickets[i].View, SetShaderResourceFlags.None);
            _outDataVariable.Set(outputBufferTickets[i].View, SetShaderResourceFlags.None);
            
            RenderContext.Current.ImmediateContext.CommitShaderResources(_srb, ResourceStateTransitionMode.Transition);
        
            var groupsX = (entitiesThisPage + threadsPerGroup - 1) / threadsPerGroup;
            RenderContext.Current.ImmediateContext.DispatchCompute(new DispatchComputeAttribs
            {
                ThreadGroupCountX = (uint)groupsX,
                ThreadGroupCountY = 1,
                ThreadGroupCountZ = 1
            });
        }
        
        _instanceOutputBuffer.DownloadLatestState(FrontQueue.Count);
        RenderContext.Current.ImmediateContext.EnqueueSignal(_fence, ++_fenceValue);
        
        _activeBufferIndex = 1 - _activeBufferIndex;
        FrontQueue.Clear();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _constantBuffer.Dispose();
        _instanceEntryBuffer.Dispose();
        _instanceOutputBuffer.Dispose();
        _fence.Dispose();
        _srb.Dispose();
        _pipelineState.Dispose();
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct ConstantParams(int count, ICamera.Plane[] planes)
    {
        public readonly uint Count = (uint)count;
        public readonly Vector3Float _padding = new(0, 0, 0);
        public readonly PlaneData LeftPlane = new(planes[0]);
        public readonly PlaneData RightPlane = new(planes[1]);
        public readonly PlaneData TopPlane = new(planes[2]);
        public readonly PlaneData BottomPlane = new(planes[3]);
        public readonly PlaneData NearPlane = new(planes[4]);
        public readonly PlaneData FarPlane = new(planes[5]);
        
        public static uint SizeInBytes => (uint)Unsafe.SizeOf<ConstantParams>();
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct PlaneData(ICamera.Plane plane)
    {
        public readonly Vector3Float Normal = plane.Normal.Downgrade();
        public readonly float D = (float)plane.D;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly struct InstanceEntry(Vector3 position, double boundingSphereRadius)
    {
        public readonly Vector3Float Position = position.Downgrade();
        public readonly float BoundingSphereRadius = (float)boundingSphereRadius;
    }
}