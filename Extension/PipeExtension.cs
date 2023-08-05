namespace JetbrainsSpaceToNotion.Extension;

public static class PipeExtension
{
    public static TOutput Pipe<TInput, TOutput>(this TInput input, Func<TInput, TOutput> func)
    {
        return func(input);
    }
}