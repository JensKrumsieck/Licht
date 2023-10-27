using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using Licht.Rendering;
using Licht.Vulkan.Pipelines;
using Silk.NET.Vulkan;
using CommandBuffer = Licht.Vulkan.CommandBuffer;

namespace Licht.Scene;

[With(typeof(TransformComponent))]
[With(typeof(MeshComponent))]
public class RenderingSystem : AEntitySetSystem<CommandBuffer>
{
    private readonly PipelineEffect _effect;
    public RenderingSystem(PipelineEffect effect, World world, IParallelRunner runner) : base(world, runner) 
        => _effect = effect;

    protected override unsafe void Update(CommandBuffer cmd, in Entity entity)
    {
        ref var t = ref entity.Get<TransformComponent>();
        ref var m = ref entity.Get<MeshComponent>();
        var pushData = new TransformationNormalPushConstants {ModelMatrix = t.TransformationMatrix(), NormalMatrix = t.NormalMatrix()};
        cmd.PushConstants(_effect, ShaderStageFlags.FragmentBit | ShaderStageFlags.VertexBit, 0, (uint)sizeof(TransformationNormalPushConstants), &pushData);
        //push constant t
        m.Mesh.Bind(cmd);
        m.Mesh.Draw(cmd);
    }
}
