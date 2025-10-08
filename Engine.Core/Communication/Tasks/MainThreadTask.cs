using System.Reflection;
using Engine.Core.Logging;

namespace Engine.Core.Communication.Tasks;

public class MarshaledTask(string homeThreadName)
{
    private List<QueuedTask> _queue = [];
    public void Run(Action action, string label, Assembly sourceAssembly)
    {
        if (Thread.CurrentThread.Name == homeThreadName)
        {
            action();
            return;
        }
        lock (_queue)
        {
            _queue.Add(new QueuedTask
            {
                SourceAssembly = sourceAssembly,
                Action = action
            });
        }
    }

    public void ExecuteAllQueued()
    {
        lock (_queue)
        {
            _queue.ForEach(task =>
            {
                try
                {
                    task.Action();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error executing main thread task: {ex.Message}");
                    Console.Error.WriteLine(ex);
                }
            });
            _queue.Clear();
        }
    }
    
    public void Purge(Assembly assembly)
    {
        lock (_queue)
        {
            _queue = _queue
                .Where(task => task.SourceAssembly != assembly)
                .ToList();
        }
    }
}

public static class MainThreadTask
{
    private static readonly MarshaledTask Handle = new("MainThread");
    public static void Run(Action action) => Handle.Run(action, "", Assembly.GetCallingAssembly());
    public static void ExecuteAllQueued() => Handle.ExecuteAllQueued();
    public static void Purge(Assembly assembly) => Handle.Purge(assembly);
}

public static class RenderThreadTask
{
    private static readonly MarshaledTask Handle = new("RenderThread");
    public static void Run(string label, Action action) => Handle.Run(action, label, Assembly.GetCallingAssembly());
    public static void ExecuteAllQueued() => Handle.ExecuteAllQueued();
    public static void Purge(Assembly assembly) => Handle.Purge(assembly);
}

internal struct QueuedTask
{
    public Assembly SourceAssembly { get; set; }
    public Action Action { get; set; }
}