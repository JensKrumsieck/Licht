namespace Licht.Applications;

public interface IApplication : IDisposable
{
    public void Run();

    public void Update(float deltaTime);
}
