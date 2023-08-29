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
        var pushConstants = new PushConstantRange(ShaderStageFlags.ComputeBit,0,(uint) sizeof(BlurSettings));
        _postProcessEffect = ShaderEffect.BuildComputeEffect(_device.Device, "./Shaders/gaussian.comp.spv",
            setLayouts, pushConstants);
        _postProcessPass = new ComputePass(_device.Device, _postProcessEffect);
    }

    public Texture FinalImage => Settings.Blur ? _outputTexture! : _texture!;

    private HitPayload TraceRay(Ray ray)
    {
        var objectIndex = -1;
        var hitDistance = float.MaxValue;

        for (var i = 0; i < _activeScene.Objects.Count; i++)
        {
            var shape = _activeScene.Objects[i];
            var closestT = shape.ClosestT(ref ray);
            if (closestT > 0.0f && closestT < hitDistance)
            {
                hitDistance = closestT;
                objectIndex = i;
            }
        }

        if (objectIndex < 0) return RayMiss(ref ray);
        return ClosestHit(ref ray, hitDistance, objectIndex);
        
    }

    private HitPayload ClosestHit(ref Ray ray, float hitDistance, int objectIndex)
    {
        var shape = _activeScene.Objects[objectIndex];
        var origin = ray.Origin - shape.Position;
        var hitPoint = origin + ray.Direction * hitDistance;
        var normal = shape.Normal(hitPoint);
        return new HitPayload
        {
            ObjectIndex = objectIndex,
            HitDistance = hitDistance,
            WorldNormal = normal,
            WorldPosition = hitPoint + shape.Position
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
                if(Settings.UseWorld)
                    light += Settings.WorldColor * contribution;
                break;
            }
            
            var shape = _activeScene.Objects[payload.ObjectIndex];
            var material = _activeScene.Materials[shape.MaterialIndex];

            ray.Origin = payload.WorldPosition + payload.WorldNormal * 0.001f;

            var specularChance = material.Specular;
            if (specularChance > 0.0f)
                specularChance = Fresnel(1.0f, 1.0f, ray.Direction, payload.WorldNormal, material.Specular, 1.0f);
            
            var doSpecular = (Random.Float(ref seed) < specularChance ? 1.0f: 0.0f);
            var diffuseRayDir = Vector3.Normalize(payload.WorldNormal + Random.InHemisphere(ref seed, payload.WorldNormal));
            var specularRayDir = ray.Direction - 2 * Vector3.Dot(payload.WorldNormal, ray.Direction) * payload.WorldNormal;
            specularRayDir = Vector3.Normalize(Vector3.Lerp(specularRayDir, diffuseRayDir, material.Roughness * material.Roughness));
            ray.Direction = Vector3.Lerp(diffuseRayDir, specularRayDir, doSpecular);
            
            light += material.Emission * contribution;
            contribution *= Vector3.Lerp(material.Albedo, material.Specular * material.Albedo, doSpecular);
            
        }
        return new Vector4(light, 1);
    }

    private static float Fresnel(float n1, float n2, Vector3 normal, Vector3 incident, float f0, float f90)
    {
        var r0 = (n1 - n2) / (n1 + n2);
        r0 *= r0;
        var cosX = -Vector3.Dot(normal, incident);
        if (n1 > n2)
        {
            var n = n1 / n2;
            var sinT2 = n * n * (1 - cosX * cosX);
            if (sinT2 > 1.0f) return f90;
            cosX = MathF.Sqrt(1.0f - sinT2);
        }

        var ret = r0 + (1.0f - r0) * MathF.Pow(1 - cosX, 5);
        return f0 * (1 - ret) + f90 * ret;
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
                
                accumulatedColor = AcesToneMapping(accumulatedColor);
                accumulatedColor = GammaCorrection(accumulatedColor, 2.4f);

                _imageData[x + y * _texture.Width] = Utils.ConvertToAbgr(ref accumulatedColor);
            }
        });
        
        fixed(uint* pImageData = _imageData)
            _texture.SetData(pImageData);
        
        if (Settings.Accumulate) _frameIndex++;
        else _frameIndex = 1;
        
        if(!Settings.Blur) return;
        //call blur compute shader
        _device.TransitionImageLayout(_outputTexture!.Image, _outputTexture.ImageFormat, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, 1, 1);
        _device.TransitionImageLayout(_outputTexture.Image, _outputTexture.ImageFormat, ImageLayout.TransferDstOptimal,
            ImageLayout.General, 1, 1);
        
        var cmd = _device.BeginSingleTimeCommands();
        cmd.BindComputePipeline(_postProcessPass);
        var blurSettings = Settings.BlurSettings;
        cmd.PushConstants(_postProcessEffect, ShaderStageFlags.ComputeBit, 0, (uint) sizeof(BlurSettings), &blurSettings);
        cmd.BindComputeDescriptorSet(_descriptorSet, _postProcessEffect);
        cmd.Dispatch(_outputTexture!.Width/8, _outputTexture.Height/8, 1);
        _device.EndSingleTimeCommands(cmd);

    }
    private static Vector4 GammaCorrection(Vector4 color, float gamma = 2f)
    {
        var exponent = 1f / gamma;
        return new Vector4(
            MathF.Pow(color.X, exponent),
            MathF.Pow(color.Y, exponent),
            MathF.Pow(color.Z, exponent),
            MathF.Pow(color.W, exponent));
    }
    
    private static Vector4 AcesToneMapping(Vector4 color)
    {
        color *= 0.6f;
        float a = 2.51f;
        float b = 0.03f;
        float c = 2.43f;
        float d = 0.59f;
        float e = 0.14f;
        return Vector4.Clamp(
            (color * (a * color + b * Vector4.One)) / (color * (c * color + d * Vector4.One) + e * Vector4.One), Vector4.Zero, Vector4.One);
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
