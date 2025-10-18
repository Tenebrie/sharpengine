namespace Engine.Tooling.Weaver;

internal static class Weaver
{
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: Engine.Tooling.Weaver <assembly-path>");
            return 1;
        }

        var assemblyPath = args[0];
        WeaverTask.Execute(assemblyPath);
        
        return 0;
    }
}