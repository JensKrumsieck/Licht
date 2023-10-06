using Licht.Applications;
using Licht.Core;
using Licht.Core.Graphics;
using Licht.Vulkan;
using Microsoft.Extensions.Logging;

var opts = ApplicationSpecification.Default;

var appBuilder = new ApplicationBuilder();
appBuilder.Services.RegisterSingleton<ILogger, Logger>();
appBuilder.Services.RegisterSingleton<IRenderer, VkRenderer>();

using var app = appBuilder.Build<WindowedApplication>(opts);

app.Run();
