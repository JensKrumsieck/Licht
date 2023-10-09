using Licht;
using Licht.Applications;
using Licht.Core;
using Licht.Vulkan.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var opts = ApplicationSpecification.Default;

var builder = new ApplicationBuilder(opts);

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddWindow(opts);
builder.Services.AddVulkanRenderer();
//use the simple allocator (only one yet!)
builder.Services.AddSingleton<IAllocator, PassthroughAllocator>();

using var app = builder.Build<WindowedApplication>();

app.Run();
