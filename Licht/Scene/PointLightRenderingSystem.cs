using System.Numerics;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Licht.Rendering;
using Licht.Vulkan;

namespace Licht.Scene;

[With(typeof(PointLightComponent))]
[With(typeof(TransformComponent))]
public unsafe class PointLightRenderingSystem : AEntitySetSystem<float>
{
    private readonly VkBuffer _storageBuffer;
    private readonly PointLightData[] _lightData;
    private int _i;
    
    public PointLightRenderingSystem(VkBuffer storageBuffer, World world, IParallelRunner runner) : base(world, runner)
    {
        _storageBuffer = storageBuffer;
        _lightData = new PointLightData[_storageBuffer.BufferSize / (ulong) sizeof(PointLightData)];
    }

    protected override void PreUpdate(float deltaTime)
    {
        base.PreUpdate(deltaTime);
        _i = 0;
    }

    protected override void Update(float deltaTime, in Entity entity)
    {
        base.Update(deltaTime, in entity);
        var t = entity.Get<TransformComponent>();
        var l = entity.Get<PointLightComponent>();
        var data = new PointLightData
        {
            PositionRadius = new Vector4(t.Translation, t.Scale.X),
            Color = new Vector4(l.Color, l.Intensity)
        };
        _lightData[_i] = data;
        _i++;
    }

    protected override void PostUpdate(float deltaTime)
    {
        base.PostUpdate(deltaTime);
        fixed (PointLightData* pLightData = _lightData)
            _storageBuffer.WriteToBuffer(pLightData);
    }
}