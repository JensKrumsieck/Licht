using System.Numerics;
using Catalyst;
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using Catalyst.Pipeline;
using Silk.NET.Vulkan;
using DescriptorPool = Catalyst.DescriptorPool;
using DescriptorSet = Catalyst.DescriptorSet;
using DescriptorSetLayout = Catalyst.DescriptorSetLayout;
using Random = Catalyst.Engine.Core.Random;

namespace RayTracing;

public sealed unsafe class Renderer : IDisposable
{
    private readonly GraphicsDevice _device;
    private Texture? _texture;
    private uint[]? _imageData;

    public Settings Settings { get; } = new();
    
    private Vector4[]? _accumulationData;
    private uint _frameIndex = 1;
    
    private Scene _activeScene;
    private Camera _activeCamera;

    private ComputePass _postProcessPass;
    private readonly ShaderEffect _postProcessEffect;
    private readonly DescriptorPool _descriptorPool;
    private readonly DescriptorSetLayout _descriptorSetLayout;
    private DescriptorSet _descriptorSet;
    private Texture? _outputTexture;
    
    public Renderer(GraphicsDevice device)
    {
        _device = device; 
        _descriptorPool = new DescriptorPool(_device.Device, new[] {new DescriptorPoolSize(DescriptorType.StorageImage, 1000)}, 1000);
        _descriptorSetLayout = DescriptorSetLayoutBuilder
            .Start()
            .With(0, DescriptorType.StorageImage, ShaderStageFlags.ComputeBit)
            .With(1, DescriptorType.StorageImage, ShaderStageFlags.ComputeBit)
            .CreateOn(_device.Device);
        var setLayouts = new[] {_descriptorSetLayout};
        _descriptorSet = _descriptorPool.AllocateDescriptorSet(setLayouts);
        _postProcessEffect = ShaderEffect.BuildComputeEffect(_device.Device, "./Shaders/gaussian.comp.spv",
            setLayouts);
        _postProcessPass = new ComputePass(_device.Device, _postProcessEffect);
    }

    public Texture FinalImage => _outputTexture!;

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
        var seed = (uint) (x + y * _texture!.Width);
        seed *= _frameIndex;
        var direction = _activeCamera.GetRayDirection((int) (x + y * _texture!.Width));
        var ray = new Ray(_activeCamera.Position, direction);
        
        var light = Vector3.Zero;
        var contribution = Vector3.One;

        var bounces = 5;

        for (var i = 0; i < bounces; i++)
        {
            seed += (uint) i;
            var payload = TraceRay(ray);
            if (payload.HitDistance < 0.0f)
            {
                var skyColor = new Vector3(.6f, .7f, .9f);
                light += skyColor * contribution;
                break;
            }
            
            var sphere = _activeScene.Spheres[payload.ObjectIndex];
            var material = _activeScene.Materials[sphere.MaterialIndex];
            
            contribution *= material.Albedo;
            light += material.Emission;
            
            ray.Origin = payload.WorldPosition + payload.WorldNormal * 0.001f;
            //var reflectNormal = payload.WorldNormal + material.Roughness * new Vector3(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle());
            //ray.Direction = ray.Direction - 2 * Vector3.Dot(reflectNormal, ray.Direction) * reflectNormal; //glm reflect impl
            ray.Direction = Vector3.Normalize(payload.WorldNormal + Random.InUnitSphere(ref seed));
        }
        return new Vector4(light, 1);
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
            _texture = new Texture(_device, newWidth, newHeight, Format.R8G8B8A8Unorm, ImageLayout.General);

        if (_outputTexture is not null)
        {
            if (_outputTexture.Width == newWidth && _outputTexture.Height == newHeight)
                return;
            _outputTexture.Resize(newWidth, newHeight);
        }
        else
            _outputTexture = new Texture(_device, newWidth, newHeight, Format.R8G8B8A8Unorm, ImageLayout.General);
        
        _descriptorPool.UpdateDescriptorSetImage(ref _descriptorSet, _texture.ImageInfo, DescriptorType.StorageImage);
        _descriptorPool.UpdateDescriptorSetImage(ref _descriptorSet, _outputTexture.ImageInfo, DescriptorType.StorageImage, 1);
        
        Application.GetApplication().UnloadUITexture(_outputTexture);
        Application.GetApplication().LoadUITexture(_outputTexture);

        Application.GetApplication().UnloadUITexture(_texture);
        Application.GetApplication().LoadUITexture(_texture);
        
        _imageData = new uint[newWidth * newHeight];
        _accumulationData = new Vector4[newWidth * newHeight];
        ResetFrameIndex();
    }
    
    public void Render(Scene scene, Camera cam)
    {
        if(_texture is null || _imageData is null || _accumulationData is null) return;

        _activeCamera = cam;
        _activeScene = scene;
        if(_frameIndex == 1) Array.Clear(_accumulationData);

        Parallel.For(0, (int)_texture.Height,y =>
        {
            for (var x = 0; x < _texture.Width; x++)
            {
                var color = RayGen(x, y);
                _accumulationData[x + y * _texture.Width] += color;
                var accumulatedColor = _accumulationData[x + y * _texture.Width] / _frameIndex;

                accumulatedColor = Vector4.Clamp(accumulatedColor, Vector4.Zero, Vector4.One);
                _imageData[x + y * _texture.Width] = Utils.ConvertToAbgr(ref accumulatedColor);
            }
        });
        
        fixed(uint* pImageData = _imageData)
            _texture.SetData(pImageData);
        
        _device.TransitionImageLayout(_outputTexture.Image, _outputTexture.ImageFormat, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, 1, 1);
        _device.TransitionImageLayout(_outputTexture.Image, _outputTexture.ImageFormat, ImageLayout.TransferDstOptimal,
            ImageLayout.General, 1, 1);
        
        var cmd = _device.BeginSingleTimeCommands();
        cmd.BindComputePipeline(_postProcessPass);
        
        cmd.BindComputeDescriptorSet(_descriptorSet, _postProcessEffect);
        cmd.Dispatch(_outputTexture!.Width/16, _outputTexture.Height/16, 1);
        _device.EndSingleTimeCommands(cmd);

        if (Settings.Accumulate) _frameIndex++;
        else _frameIndex = 1;
    }

    public void ResetFrameIndex() => _frameIndex = 1;
    
    public void Dispose()
    {
        _texture?.Dispose();
        _outputTexture?.Dispose();
        
        _descriptorPool.Dispose();
        _descriptorSetLayout.Dispose();
        _postProcessEffect.Dispose();
        _postProcessPass.Dispose();
    }
}
