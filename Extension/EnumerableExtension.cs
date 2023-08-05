namespace JetbrainsSpaceToNotion.Extension;

internal static class EnumerableExtension
{
    internal static IEnumerable<T> Cross<T>(this IEnumerable<T> source, IEnumerable<T> following)
    {
        using var enumerator = source.GetEnumerator();
        using var followingEnumerator = following.GetEnumerator();

        while (true)
        {
            var elementLeft = true;
            var followingElementLeft = true;

            if (enumerator.MoveNext())
                yield return enumerator.Current;
            else
                elementLeft = false;

            if (followingEnumerator.MoveNext())
                yield return followingEnumerator.Current;
            else
                followingElementLeft = false;

            if (!elementLeft && !followingElementLeft)
                yield break;
        }
    }

    internal static List<T> Subtract<T>(this IEnumerable<T> source, IEnumerable<T> target)
    {
        var list = source.ToList();
        foreach (var item in target)
            list.Remove(item);

        return list;
    }
}