namespace Elsa.FakeData.Extensions;

internal static class ObjectExtensions
{
    public static T When<T>(this T obj, bool condition, Func<T, T> action)
    {
        return condition ? action(obj) : obj;
    }
}
