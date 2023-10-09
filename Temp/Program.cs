using Licht;
using Licht.Applications;
using Licht.Core.Graphics;
using Licht.Vulkan;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var opts = ApplicationSpecification.Default;

var builder = new ApplicationBuilder();
builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddSingleton<IRenderer, VkRenderer>();
builder.Services.AddSingleton<Window>(l => new Window(l.GetService<ILogger>()!, opts.ApplicationName, opts.Width, opts.Height, opts.IsFullscreen));

using var app = builder.Build<WindowedApplication>(opts);

app.Run();
