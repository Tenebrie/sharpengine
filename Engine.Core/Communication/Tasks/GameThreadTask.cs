using System.Reflection;
using Engine.Core.Logging;

namespace Engine.Core.Communication.Tasks;

public class MarshaledTask(string homeThreadName)
{
    private long _taskIdCounter = 0;
    private List<QueuedTask> _queue = [];
    private List<QueuedTask> _nextFrameQueue = [];
    public long Run(Action action, string label, Assembly sourceAssembly)
    {
        if (Thread.CurrentThread.Name == homeThreadName)
        {
            action();
            return -1;
        }

        lock (_queue)
        {
            var taskId = _taskIdCounter;
            _queue.Add(new QueuedTask
            {
                Id = _taskIdCounter,
                SourceAssembly = sourceAssembly,
                Action = action
            });
            _taskIdCounter += 1;
            return taskId;
        }
    }
    public long NextFrame(Action action, string label, Assembly sourceAssembly)
    {
        lock (_nextFrameQueue)
        {
            var taskId = _taskIdCounter;
            _nextFrameQueue.Add(new QueuedTask
            {
                Id = _taskIdCounter,
                SourceAssembly = sourceAssembly,
                Action = action
            });
            _taskIdCounter += 1;
            return taskId;
        }
    }
    public void Cancel(long taskId)
    {
        lock (_queue)
        {
            _queue = _queue
                .Where(task => task.Id != taskId)
                .ToList();
        }
    }

    public void ExecuteAllQueued()
    {
        lock (_queue)
        {
            for (var index = 0; index < _queue.Count; index++)
            {
                var task = _queue[index];
                try
                {
                    task.Action();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error executing marshalled task: {ex.Message}");
                    Console.Error.WriteLine(ex);
                }
            }

            _queue.Clear();

            lock (_nextFrameQueue)
            {
                _queue.AddRange(_nextFrameQueue);
                _nextFrameQueue.Clear();
            }
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
    public static long Run(Action action) => Handle.Run(action, "", Assembly.GetCallingAssembly());
    public static long NextFrame(Action action) => Handle.NextFrame(action, "", Assembly.GetCallingAssembly());
    public static void Cancel(long taskId) => Handle.Cancel(taskId);
    public static void ExecuteAllQueued() => Handle.ExecuteAllQueued();
    public static void Purge(Assembly assembly) => Handle.Purge(assembly);
}

public static class GameThreadTask
{
    private static readonly MarshaledTask Handle = new("GameThread");
    public static long Run(Action action) => Handle.Run(action, "", Assembly.GetCallingAssembly());
    public static long NextFrame(Action action) => Handle.NextFrame(action, "", Assembly.GetCallingAssembly());
    public static void Cancel(long taskId) => Handle.Cancel(taskId);
    public static void ExecuteAllQueued() => Handle.ExecuteAllQueued();
    public static void Purge(Assembly assembly) => Handle.Purge(assembly);
}

public static class RenderThreadTask
{
    private static readonly MarshaledTask Handle = new("RenderThread");
    public static long Run(string label, Action action) => Handle.Run(action, label, Assembly.GetCallingAssembly());
    public static void Cancel(long taskId) => Handle.Cancel(taskId);
    public static void ExecuteAllQueued() => Handle.ExecuteAllQueued();
    public static void Purge(Assembly assembly) => Handle.Purge(assembly);
}

internal record struct QueuedTask
{
    public required long Id { get; init; }
    public required Assembly SourceAssembly { get; init; }
    public required Action Action { get; init; }
}