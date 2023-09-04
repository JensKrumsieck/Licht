using System.Numerics;
using System.Runtime.CompilerServices;
using Catalyze;
using Catalyze.Allocation;
using Catalyze.Applications;
using Catalyze.Pipeline;
using Catalyze.Tools;
using Catalyze.UI;
using ComputeShader;
using ImGuiNET;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;
using DescriptorPool = Catalyze.DescriptorPool;
using DescriptorSet = Catalyze.DescriptorSet;
using DescriptorSetLayout = Catalyze.DescriptorSetLayout;

var builder = Application.CreateBuilder();
var window = builder.Services.AddWindowing(WindowOptions.DefaultVulkan);
builder.Services.AddInput(window);
builder.Services.RegisterSingleton<IAllocator, PassthroughAllocator>();

var app = builder.Build();
var options = new GraphicsDeviceCreateOptions();
options.PhysicalDeviceSelector = SelectorTools.DefaultGpuSelector;
app.UseVulkan(options)
    .UseImGui();
app.AttachLayer<ComputeShaderAppLayer>();
app.Run();

namespace ComputeShader
{
    public struct BlurSettings
    {
        public int KernelSize = 3;
        public float Sigma = 1f;
        public BlurSettings() {}
    }

    public unsafe class ComputeShaderAppLayer : IAppLayer
    {
        private Renderer _renderer = null!;
        private ShaderEffect _shaderEffect;
        private ShaderPass _shaderPass;

        public BlurSettings BlurSettings = new();
    
        private ComputePass _postProcessPass;
        private ShaderEffect _postProcessEffect;
    
        private DescriptorPool _descriptorPool;
        private DescriptorSetLayout _descriptorSetLayout;
        private DescriptorSet _descriptorSet;
        private Texture _texture = null!;
        public Texture OutputTexture = null!;
    
        public void OnAttach()
        {
            _renderer = Application.GetInstance().GetModule<Renderer>()!;
            _descriptorPool = new DescriptorPool(_renderer.Device.VkDevice, new[] {new DescriptorPoolSize(DescriptorType.StorageImage, 1000)}, 1000);
            _descriptorSetLayout = DescriptorSetLayoutBuilder
                .Start()
                .With(0, DescriptorType.StorageImage, ShaderStageFlags.ComputeBit)
                .With(1, DescriptorType.StorageImage, ShaderStageFlags.ComputeBit)
                .CreateOn(_renderer.Device.VkDevice);
            var setLayouts = new[] {_descriptorSetLayout};
            _descriptorSet = _descriptorPool.AllocateDescriptorSet(setLayouts);
            var pushConstants = new PushConstantRange(ShaderStageFlags.ComputeBit,0,(uint) Unsafe.SizeOf<BlurSettings>());
            _postProcessEffect = ShaderEffect.BuildComputeEffect(_renderer.Device.VkDevice, "./Shaders/gaussian.comp.spv",
                setLayouts, pushConstants);
            _postProcessPass = new ComputePass(_renderer.Device.VkDevice, _postProcessEffect);

            _texture = new Texture(_renderer.Device, "./Assets/atomium.jpg", ImageLayout.General);
            OutputTexture = new Texture(_renderer.Device, _texture.Width, _texture.Height, Format.R8G8B8A8Unorm, ImageLayout.General);
        
            _renderer.Device.TransitionImageLayout(OutputTexture!.Image, OutputTexture.ImageFormat, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, 1, 1);
            _renderer.Device.TransitionImageLayout(OutputTexture.Image, OutputTexture.ImageFormat, ImageLayout.TransferDstOptimal, ImageLayout.General, 1, 1);
            _descriptorPool.UpdateDescriptorSetImage(ref _descriptorSet, _texture.ImageInfo, DescriptorType.StorageImage);
            _descriptorPool.UpdateDescriptorSetImage(ref _descriptorSet, OutputTexture.ImageInfo, DescriptorType.StorageImage, 1);
            Application.GetInstance().GetModule<ImGuiRenderer>()!.LoadTexture(OutputTexture);
        }

        public void OnUpdate(double deltaTime)
        {
            var cmd = _renderer.Device.BeginSingleTimeCommands();
            cmd.BindComputePipeline(_postProcessPass);
            var blurSettings = BlurSettings;
            cmd.PushConstants(_postProcessEffect, ShaderStageFlags.ComputeBit, 0, (uint) sizeof(BlurSettings), &blurSettings);
            cmd.BindComputeDescriptorSet(_descriptorSet, _postProcessEffect);
            cmd.Dispatch(OutputTexture.Width/8, OutputTexture.Height/8);
            _renderer.Device.EndSingleTimeCommands(cmd);
        }

        public void OnDrawGui(double deltaTime)
        {
            ImGui.DockSpaceOverViewport();
            ImGui.Begin("Settings");
            ImGui.DragFloat("Blur Sigma", ref BlurSettings.Sigma, .01f, 0, float.MaxValue);
            ImGui.DragInt("Blur Kernel", ref BlurSettings.KernelSize, 1, 0, int.MaxValue);
            ImGui.End();
        
            ImGui.Begin("Image");
            //fit image into viewport
            var availWidth = ImGui.GetContentRegionAvail().X;
            var availHeight = ImGui.GetContentRegionAvail().Y;
            var facW = OutputTexture.Width / availWidth;
            var facH = OutputTexture.Height / availHeight;
            var fac = Math.Max(facW, facH);
            ImGui.Image((nint) OutputTexture.DescriptorSet.Handle, new Vector2(OutputTexture.Width / fac, OutputTexture.Height / fac), new Vector2(1,0), new Vector2(0,1));
            ImGui.End();
        }

        public void OnDetach()
        {
            _descriptorPool.Dispose();
            _descriptorSetLayout.Dispose();
            _postProcessEffect.Dispose();
            _postProcessPass.Dispose();
            _shaderEffect.Dispose();
            _shaderPass.Dispose();
        }
    }
}