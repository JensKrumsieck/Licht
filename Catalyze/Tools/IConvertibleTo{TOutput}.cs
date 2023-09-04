namespace Catalyze.Tools;

public interface IConvertibleTo<out TOutput> where TOutput : unmanaged
{
    TOutput Convert();
}