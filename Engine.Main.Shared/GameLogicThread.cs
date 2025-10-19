using System.Diagnostics;
using Engine.Core.Communication.Tasks;
using Engine.Core.Logging;

namespace Engine.Main.Shared;

public class GameLogicThread(IEntryPoint entryPoint)
{
    private Thread? _threadHandle;
    private enum ThreadState { Stopped, Running, Pausing, Paused }
    private volatile ThreadState _threadState = ThreadState.Stopped;
    
    public void Start()
    {
        if (_threadHandle is { IsAlive: true } && _threadState == ThreadState.Running)
            return;
        
        _threadState = ThreadState.Running;
        _threadHandle = new Thread(ThreadLoop)
        {
            Name = "GameLogicThread",
            IsBackground = true
        };
        _threadHandle.Start();
    }
    
    private void ThreadLoop()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            double lastFrameTime = 0;
            
        
            while (_threadState != ThreadState.Stopped)
            {
                entryPoint.RenderingAssembly.StartFrameRender();
                if (_threadState == ThreadState.Stopped)
                    break;
                if (_threadState == ThreadState.Pausing)
                {
                    _threadState = ThreadState.Paused;
                    while (_threadState == ThreadState.Paused)
                        Thread.Sleep(1);
                }
                
                MainThreadTask.ExecuteAllQueued();
                
                var currentTime = stopwatch.Elapsed.TotalMicroseconds;
                var deltaTime = (currentTime - lastFrameTime) / 1000000.0;
                lastFrameTime = currentTime;
                
                foreach (var guestAssembly in entryPoint.GuestAssemblies)
                {
                    guestAssembly.Update(deltaTime);
                }
                
                entryPoint.RenderingAssembly.WaitUntilFrameEnd();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"GameLogicThread: Thread crashed: {ex}");
            throw;
        }
    }

    public void Pause()
    {
        _threadState = ThreadState.Pausing;
        while (_threadState != ThreadState.Paused)
        {
            Thread.Sleep(1);
        }
    }
    public void Resume()
    {
        if (_threadState != ThreadState.Paused)
            return;
        _threadState = ThreadState.Running;
    }

    public void Stop()
    {
        _threadState = ThreadState.Stopped;
        _threadHandle?.Join();
        _threadHandle = null;
    }
}