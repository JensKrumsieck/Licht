using System.Numerics;
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using Silk.NET.Vulkan;

namespace RayTracing;

public sealed unsafe class Renderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private Texture? _texture;
    private uint[]? _imageData;

    public Settings Settings { get; private set; }
    
    private Vector4[]? _accumulationData;
    private uint _frameIndex = 1;
    
    private readonly Random _random = new Random();

    private Scene _activeScene;
    private Camera _activeCamera;
    
    public Renderer(GraphicsDevice device) => _device = device;

    public Texture FinalImage => _texture!;

    private HitPayload TraceRay(Ray ray)
    {
        var closestSphere = -1;
        var hitDistance = float.MaxValue;

        for (var i = 0; i < _activeScene.Spheres.Count; i++)
        {
            var sphere = _activeScene.Spheres[i];
            var origin = ray.Origin - sphere.Position;
            var radius = sphere.Radius;

            var a = Vector3.Dot(ray.Direction, ray.Direction);
            var b = 2.0f * Vector3.Dot(origin, ray.Direction);
            var c = Vector3.Dot(origin, origin) - radius * radius;

            var discriminant = b * b - 4 * a * c;

            if (discriminant < 0) continue;

            //var t0 = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
            var closestT = (-b - MathF.Sqrt(discriminant)) / (2.0f * a);
            if (closestT > 0.0f && closestT < hitDistance)
            {
                hitDistance = closestT;
                closestSphere = i;
            }
        }

        if (closestSphere < 0) return RayMiss(ref ray);
        return ClosestHit(ref ray, hitDistance, closestSphere);
        
    }

    private HitPayload ClosestHit(ref Ray ray, float hitDistance, int objectIndex)
    {
        var sphere = _activeScene.Spheres[objectIndex];
        var origin = ray.Origin - sphere.Position;
        var hitPoint = origin + ray.Direction * hitDistance;
        var normal = Vector3.Normalize(hitPoint);
        return new HitPayload
        {
            ObjectIndex = objectIndex,
            HitDistance = hitDistance,
            WorldNormal = normal,
            WorldPosition = hitPoint + sphere.Position
        };
    }

    private Vector4 RayGen(int x, int y)
    {
        var direction = _activeCamera.GetRayDirection((int) (x + y * _texture!.Width));
        var ray = new Ray(_activeCamera.Position, direction);
        
        var color = Vector3.Zero;
        var multiplier = 1.0f;

        var bounces = 5;
        for (var i = 0; i < bounces; i++)
        {
            var payload = TraceRay(ray);
            if (payload.HitDistance < 0.0f)
            {
                var skyColor = new Vector3(.6f, .7f, .9f);
                color += skyColor * multiplier;
                break;
            }
        
            var lightDir = Vector3.Normalize(-Vector3.One);
            var lightIntensity = MathF.Max(Vector3.Dot(payload.WorldNormal, -lightDir), 0.0f);
            
            var sphere = _activeScene.Spheres[payload.ObjectIndex];
            var material = _activeScene.Materials[sphere.MaterialIndex];
            var sphereColor = material.Albedo;
            sphereColor *= lightIntensity;
            color += sphereColor * multiplier;
            
            multiplier *= .5f;
            
            ray.Origin = payload.WorldPosition + payload.WorldNormal * 0.001f;
            var reflectNormal = payload.WorldNormal + material.Roughness * new Vector3(_random.NextSingle(), _random.NextSingle(), _random.NextSingle());
            ray.Direction = ray.Direction - 2 * Vector3.Dot(reflectNormal, ray.Direction) * reflectNormal; //glm reflect impl
        }
        return new Vector4(color, 1);
    }

    private HitPayload RayMiss(ref Ray ray) => new()
    {
        HitDistance = -1.0f
    };

    public void OnResize(uint newWidth, uint newHeight)
    {
        if (_texture is not null)
        {
            if (_texture.Width == newWidth && _texture.Height == newHeight)
                return;

            _texture.Resize(newWidth, newHeight);
        }
        else
            _texture = new Texture(_device, newWidth, newHeight, Format.R8G8B8A8Srgb, null);

        Application.GetApplication().UnloadUITexture(_texture);
        Application.GetApplication().LoadUITexture(_texture);

        _imageData = new uint[newWidth * newHeight];
        _accumulationData = new Vector4[newWidth * newHeight];
    }
    
    public void Render(Scene scene, Camera cam)
    {
        if(_texture is null || _imageData is null || _accumulationData is null) return;

        _activeCamera = cam;
        _activeScene = scene;
        if(_frameIndex == 1) Array.Clear(_accumulationData);
        
        for (var y = 0; y < _texture.Height; y++)
        {
            for (var x = 0; x < _texture.Width ; x++)
            {
                var color = RayGen(x,y);
                _accumulationData[x + y * _texture.Width] *= color;
                color = Vector4.Clamp(color, Vector4.Zero, Vector4.One);
                _imageData[x + y * _texture.Width] = Utils.ConvertToAbgr(ref color);
            }  
        }
        fixed(uint* pImageData = _imageData)
            _texture.SetData(pImageData);

        if (Settings.Accumulate) _frameIndex++;
        else _frameIndex = 1;
    }

    public void ResetFrameIndex() => _frameIndex = 1;
    
    public void Dispose() => _texture?.Dispose();
}
