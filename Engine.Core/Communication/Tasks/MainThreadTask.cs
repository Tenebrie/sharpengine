using System.Reflection;
using Engine.Core.Logging;

namespace Engine.Core.Communication.Tasks;

public class MainThreadTask
{
    private static List<QueuedTask> _queue = [];
    public static void Run(Action action)
    {
        lock (_queue)
        {
            _queue.Add(new QueuedTask
            {
                SourceAssembly = Assembly.GetCallingAssembly(),
                Action = action
            });
        }
    }

    public static void ExecuteAllQueued()
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
    
    public static void Purge(Assembly assembly)
    {
        lock (_queue)
        {
            _queue = _queue
                .Where(task => task.SourceAssembly != assembly)
                .ToList();
        }
    }
}

internal struct QueuedTask
{
    public Assembly SourceAssembly { get; set; }
    public Action Action { get; set; }
}