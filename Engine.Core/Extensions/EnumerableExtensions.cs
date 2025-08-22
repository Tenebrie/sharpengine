namespace Engine.Core.Extensions;

public static class EnumerableExtensions
{
    public static void ForEachTry<T>(
        this IEnumerable<T> source,
        Action<T> action,
        Action<T, Exception>? onError = null)
    {
        foreach (var item in source)
        {
            try
            {
                action(item);
            }
            catch (Exception ex)
            {
                onError?.Invoke(item, ex);
            }
        }
    }
}