using ImGuiNET;
using Licht.Applications;
using Licht.Core;
using Licht.Vulkan;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Windowing;

var opts = ApplicationSpecification.Default;
var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer<PassthroughAllocator>();

{
    using var app = builder.Build<ImGuiSample>();
    app.Run();
}
sealed class ImGuiSample : ImGuiApplication
{ 
    public ImGuiSample(ILogger logger, VkRenderer renderer, IWindow window) : base(logger, renderer, window) { }

    public override void DrawUI(CommandBuffer cmd, float deltaTime)
    {        
        ImGui.ShowDemoWindow();
        base.DrawUI(cmd, deltaTime);
    }
}