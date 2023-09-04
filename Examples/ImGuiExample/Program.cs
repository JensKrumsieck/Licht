using Catalyst;
using Catalyst.Allocation;
using Catalyst.Applications;
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

public class ImGuiAppLayer : IAppLayer
{
    public void OnDrawGui(double deltaTime)
    {
        ImGui.ShowDemoWindow();
    }
}
