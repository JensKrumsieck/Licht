# Catalyze [WIP/Concept Stage]
[![NuGet Badge](https://buildstats.info/nuget/Catalyze?includePreReleases=true)](https://www.nuget.org/packages/Catalyze/0.1.0-alpha.1)


A thin but opinionated abstraction layer to write Vulkan Code in C# (using Silk.NET) with fewer boilerplate code.
(work in progress, see Examples folder for some examples)

Get to the Triangle in about 40 lines of code:
```csharp
using Catalyze;
using Catalyze.Allocation;
using Catalyze.Applications;
using Catalyze.Pipeline;
using Silk.NET.Windowing;
using TriangleExample;

var builder = Application.CreateBuilder();
builder.Services.AddWindowing(WindowOptions.DefaultVulkan);
builder.Services.RegisterSingleton<IAllocator, PassthroughAllocator>();

var app = builder.Build();
app.UseVulkan(new GraphicsDeviceCreateOptions());
app.AttachLayer<ExampleAppLayer>();
app.Run();
app.Dispose();
namespace TriangleExample
{
    internal class ExampleAppLayer : IAppLayer
    {
        private Renderer _renderer = null!;
        private ShaderEffect _shaderEffect;
        private ShaderPass _shaderPass;
    
        public void OnAttach()
        {
            _renderer = Application.GetInstance().GetModule<Renderer>()!;
            _shaderEffect = ShaderEffect.BuildEffect(_renderer.Device.VkDevice, "./Shaders/triShader.vert.spv",
                "./Shaders/triShader.frag.spv", null);
            var passInfo = ShaderPassInfo.Default();
            _shaderPass = new ShaderPass(_renderer.Device.VkDevice, _shaderEffect, passInfo, default, _renderer.RenderPass);
        }

        public void OnUpdate(double deltaTime)
        {
            _renderer.CurrentCommandBuffer.BindGraphicsPipeline(_shaderPass);
            _renderer.CurrentCommandBuffer.Draw(3, 1, 0, 0);
        }

        public void OnDetach()
        {
            _shaderEffect.Dispose();
            _shaderPass.Dispose();
        }
    }
}
```

There are currently examples for:
* Triangle Example
* ImGui Example
* Compute Shader Example
