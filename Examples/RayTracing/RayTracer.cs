using System.Numerics;
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using Silk.NET.Vulkan;

namespace RayTracing;

public sealed unsafe class RayTracer : IDisposable
{
    private readonly GraphicsDevice _device;
    private Texture? _texture;
    private uint[]? _imageData;
    public RayTracer(GraphicsDevice device) => _device = device;

    public Texture FinalImage => _texture!;

    public static Vector4 TraceRay(Scene scene, Ray ray)
    {
        if (scene.Spheres.Count == 0) return new Vector4(0, 0, 0, 1);
        var rayDirection = ray.Direction;
        rayDirection = Vector3.Normalize(rayDirection);
        
        Sphere? closestSphere = null;
        var hitDistance = float.MaxValue;
        
        foreach (var sphere in scene.Spheres)
        {
            var origin = ray.Origin - sphere.Position;
            var radius = sphere.Radius;

            var a = Vector3.Dot(rayDirection, rayDirection);
            var b = 2.0f * Vector3.Dot(origin, rayDirection);
            var c = Vector3.Dot(origin, origin) - radius * radius;

            var discriminant = b * b - 4 * a * c;

            if (discriminant < 0) continue;

            //var t0 = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
            var t1 = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
            if (t1 < hitDistance)
            {
                hitDistance = t1;
                closestSphere = sphere;
            }
        }
        if (closestSphere is null) return new Vector4(0, 0, 0, 1);
        
        var rayOrigin = ray.Origin - closestSphere.Position;
        var hitPoint = rayOrigin + rayDirection * hitDistance;
        var normal = Vector3.Normalize(hitPoint);

        var lightDir = -Vector3.One;
        lightDir = Vector3.Normalize(lightDir);
        var d = MathF.Max(Vector3.Dot(normal, -lightDir), 0);

        var sphereColor = closestSphere.Albedo;
        return new Vector4(sphereColor * d, 1);
    }
    
    public void OnResize(uint newWidth, uint newHeight)
    {
        if (_texture is not null)
        {
            if(_texture.Width == newWidth && _texture.Height == newHeight) 
                return;
            
            _texture.Resize(newWidth, newHeight);
        }
        else
            _texture = new Texture(_device, newWidth, newHeight, Format.R8G8B8A8Unorm, null);

        Application.GetApplication().UnloadUITexture(_texture);
        Application.GetApplication().LoadUITexture(_texture);

        _imageData = new uint[newWidth * newHeight];
    }
    
    public void Render(Scene scene, Camera cam)
    {
        if(_texture is null || _imageData is null) return;

        var rayOrigin = cam.Position;
        for (var y = 0; y < _texture.Height; y++)
        {
            for (var x = 0; x < _texture.Width ; x++)
            {
                var rayDir = cam.GetRayDirection((int)(x + y * _texture.Width));
                var color = TraceRay(scene, new Ray(ref rayOrigin, ref rayDir));
                color = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                _imageData[x + y * _texture.Width] = Utils.ConvertToAbgr(ref color);
            }  
        }
        fixed(uint* pImageData = _imageData)
            _texture.SetData(pImageData);
    }
    public void Dispose() => _texture?.Dispose();
}
