using Catalyze;
using Catalyze.Allocation;
using Catalyze.Applications;
using ImGuiExample;
using ImGuiNET;
using Silk.NET.Windowing;

var builder = Application.CreateBuilder();
var window = builder.Services.AddWindowing(WindowOptions.DefaultVulkan);
builder.Services.AddInput(window);
builder.Services.RegisterSingleton<IAllocator, PassthroughAllocator>();

var app = builder.Build();
app.UseVulkan(new GraphicsDeviceCreateOptions())
    .UseImGui();
app.AttachLayer<ImGuiAppLayer>();
app.Run();

namespace ImGuiExample
{
    public class ImGuiAppLayer : IAppLayer
    {
        public void OnDrawGui(double deltaTime)
        {
            ImGui.ShowDemoWindow();
        }
    }
}
