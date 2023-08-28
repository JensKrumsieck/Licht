namespace RayTracing;

public class Settings
{
    public bool Accumulate = true;
    public bool Blur = false;
    public BlurSettings BlurSettings = new();
}

public struct BlurSettings
{
    public int KernelSize = 3;
    public float Sigma = 1f;
    public BlurSettings() {}
}