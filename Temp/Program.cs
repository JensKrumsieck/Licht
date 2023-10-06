using Licht.Applications;
using Licht.Core;
using Licht.Graphics;
using Licht.Vulkan;

var opts = new ApplicationSpecification("Licht Applikation", new Version(1, 0, 0), 1600, 900, false);

var appBuilder = new ApplicationBuilder();
appBuilder.Services.RegisterSingleton<ILogger, Logger>();
appBuilder.Services.RegisterSingleton<IRenderer, VulkanRenderer>();

using var app = appBuilder.Build<WindowedApplication>(opts);

app.Run();
