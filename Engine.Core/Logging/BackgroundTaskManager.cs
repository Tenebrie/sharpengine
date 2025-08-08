namespace Engine.Core.Logging;

public static class BackgroundTaskManager
{
    private static readonly List<BackgroundTaskHandle> Handles = [];
    
    public static BackgroundTaskHandle Start()
    {
        var handle = new BackgroundTaskHandle();
        lock (Handles)
            Handles.Add(handle);
        UpdateLogger();
        return handle;
    }
    
    public static void Stop(BackgroundTaskHandle handle)
    {
        lock (Handles)
            Handles.Remove(handle);
        UpdateLogger();
    }

    private static void UpdateLogger()
    {
        int handleCount;
        lock (Handles)
            handleCount = Handles.Count;
        if (handleCount == 0)
        {
            Logger.ClearPersistent("BackgroundTasks");
            return;
        }
        Logger.ShowPersistent(LogLevel.Info,"BackgroundTasks", $"Running background tasks: {handleCount}");
    }
}

public sealed class BackgroundTaskHandle : IDisposable
{
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        BackgroundTaskManager.Stop(this);
    }
    ~BackgroundTaskHandle() => Dispose();
}
