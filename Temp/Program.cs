using Licht.Applications;
using Licht.Core;
using Licht.Vulkan;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

var opts = ApplicationSpecification.Default;
var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer<PassthroughAllocator>();

{
    using var app = builder.Build<RaytracingApplication>();
    app.Run();
}

sealed unsafe class RaytracingApplication : WindowedApplication
{
    private VkGraphicsDevice _device;
    private VkImage _image;
    public RaytracingApplication(ILogger logger, VkGraphicsDevice device, VkRenderer renderer, IWindow window) : base(logger, renderer, window)
    {
        _device = device;
        var width = renderer.Extent?.Width ?? 0;
        var height = renderer.Extent?.Height ?? 0;
        _image = new VkImage(_device, width, height, Format.B8G8R8A8Unorm, ImageLayout.General,
            ImageUsageFlags.StorageBit);
    }

    public override void Release()
    {
        base.Release();
        _image.Dispose();
    }
}