namespace Licht.Applications;

public readonly record struct ApplicationSpecification(string ApplicationName, Version ApplicationVersion, int Width,
    int Height, bool IsFullscreen)
{
    public static ApplicationSpecification Default = new("Licht", new Version(1,0,0),1600, 900, false);
}
