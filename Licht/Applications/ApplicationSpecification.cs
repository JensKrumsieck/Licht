namespace Licht.Applications;

public readonly record struct ApplicationSpecification(string ApplicationName, Version ApplicationVersion, int Width, int Height, bool IsFullscreen);
