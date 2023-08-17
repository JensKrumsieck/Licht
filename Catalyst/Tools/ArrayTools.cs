namespace Catalyst.Tools;

public static class ArrayTools
{
    public static TOutput[] AsArray<TInput, TOutput>(this TInput[] input)
        where TInput : IConvertibleTo<TOutput> where TOutput : unmanaged 
        => Array.ConvertAll(input, i => i.Convert());
}