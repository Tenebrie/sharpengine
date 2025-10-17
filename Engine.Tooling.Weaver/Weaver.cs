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

        Console.WriteLine("Beginning weaving process...");
        var assemblyPath = args[0];
        // Your weaving logic here using Mono.Cecil
        // WeaveAssembly(assemblyPath);
        WeaverTask.Execute(assemblyPath);
        
        return 0;
    }
}