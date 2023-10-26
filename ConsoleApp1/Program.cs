using System.Numerics;
using ConsoleApp1;
using Licht.Applications;
using Licht.Core;
using Licht.Vulkan;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Windowing;
using DefaultEcs;
using Licht.Rendering;
using Licht.Scene;
using Licht.Vulkan.Pipelines;
using Silk.NET.Vulkan;
using CommandBuffer = Licht.Vulkan.CommandBuffer;
using DescriptorPool = Licht.Vulkan.DescriptorPool;
using DescriptorSetLayout = Licht.Vulkan.DescriptorSetLayout;

var opts = ApplicationSpecification.Default with {ApplicationName = "Triangle"};
var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer<PassthroughAllocator>();

{
    using var app = builder.Build<Engine>();
    app.Run();
}

unsafe class Engine : WindowedApplication
{
    private readonly VkGraphicsDevice _device;
    private readonly DescriptorPool _pool;
    private readonly DescriptorSetLayout _descriptorSetLayout;
    private readonly VkGraphicsPipeline _pipeline;
    private readonly PipelineEffect _effect;
    private readonly World _scene = new();
    private readonly DescriptorSet _descriptorSet;
    private readonly VkBuffer _globalUbo;

    public Engine(ILogger logger, VkGraphicsDevice device, VkRenderer renderer, IWindow window) : base(logger, renderer, window)
    {
        _device = device;
        var poolSizes = new DescriptorPoolSize[]
        {
            new() {Type = DescriptorType.UniformBuffer, DescriptorCount = 3}
        };
        _pool = _device.CreateDescriptorPool(poolSizes, 3);
        var binding0 = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            StageFlags = ShaderStageFlags.FragmentBit | ShaderStageFlags.VertexBit
        };
        _descriptorSetLayout = _device.CreateDescriptorSetLayout(new[] {binding0});
        _descriptorSet = _pool.AllocateDescriptorSet(_descriptorSetLayout);
        _globalUbo = new VkBuffer(_device, (ulong) sizeof(Ubo), BufferUsageFlags.UniformBufferBit,
            MemoryPropertyFlags.HostVisibleBit);
        _device.UpdateDescriptorSetBuffer(ref _descriptorSet, _globalUbo.DescriptorInfo(), DescriptorType.UniformBuffer, 0);
        var pushRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit | ShaderStageFlags.FragmentBit,
            Offset = 0,
            Size = (uint) sizeof(Push)
        };
        var passDescription = GraphicsPipelineDescription.Default();
        _effect = PipelineEffect.BuildEffect(_device, "./assets/shaders/lit.vert.spv", "./assets/shaders/lit.frag.spv", new[]{_descriptorSetLayout}, pushRange);
        _pipeline = new VkGraphicsPipeline(_device, _effect, passDescription, Vertex.VertexInfo(), Renderer.RenderPass!.Value);

        var suzanne = MeshImporter.FromFile("./assets/models/suzanne.fbx")[0];
        var e = _scene.CreateEntity(); 
        e.Set(new TransformComponent());
        e.Set(new MeshComponent(suzanne.Process(device)));
    }

    public override void DrawFrame(CommandBuffer cmd, float deltaTime)
    {
        base.DrawFrame(cmd, deltaTime);
        cmd.BindGraphicsPipeline(_pipeline);
        cmd.BindGraphicsDescriptorSet(_descriptorSet, _effect);
        var ubo = new Ubo
        {
            View = Matrix4x4.Identity,
            InverseView = Matrix4x4.Identity,
            Projection = Matrix4x4.Identity
        };
        var dest = IntPtr.Zero.ToPointer();
        _globalUbo.Map(ref dest);
        _globalUbo.WriteToBuffer(&ubo, dest);
        _globalUbo.Unmap();
        //could be AEntitySetSystem<T>
        var rendered = _scene.GetEntities().With(typeof(TransformComponent), typeof(MeshComponent)).AsSet();
        foreach (var e in rendered.GetEntities())
        {
            ref var t = ref e.Get<TransformComponent>();
            ref var m = ref e.Get<MeshComponent>();

            var pushData = new Push {Model = t.TransformationMatrix(), Normal = t.NormalMatrix()};
            cmd.PushConstants(_effect, ShaderStageFlags.FragmentBit | ShaderStageFlags.VertexBit, 0, (uint)sizeof(Push), &pushData);
            //push constant t
            m.Mesh.Bind(cmd);
            m.Mesh.Draw(cmd);
        }
    }

    public override void Release()
    {
        base.Release();
        _device.WaitIdle();
        //is there a better way? there must be!
        foreach (var e in _scene.GetAll<MeshComponent>())
        {
            e.Dispose();
        }
        _scene.Dispose();
        
        _pool.FreeDescriptorSet(_descriptorSet);
        _descriptorSetLayout.Dispose();
        _pool.Dispose();
        _globalUbo.Dispose();
        _pipeline.Dispose();        
        _effect.Dispose();
    }
}