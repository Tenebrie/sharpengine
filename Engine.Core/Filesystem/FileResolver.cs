namespace Engine.Core.Filesystem;

public static class FileResolver
{
    private static readonly string BasePath = AppContext.BaseDirectory;

    public static string Resolve(string relativePath)
    {
        return Path.GetFullPath(Path.Combine(BasePath, relativePath));
    }

    public static byte[] ReadAllBytes(string path) => File.ReadAllBytes(Resolve(path));
}