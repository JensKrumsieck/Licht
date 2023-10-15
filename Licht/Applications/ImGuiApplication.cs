using System.Numerics;
using ImGuiNET;
using Licht.Vulkan;
using Licht.Vulkan.UI;
using Microsoft.Extensions.Logging;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Licht.Applications;

public class ImGuiApplication : WindowedApplication
{
    private bool _frameBegun;
    public ImGuiContext UiContext;
    public ImGuiApplication(ILogger logger, VkRenderer renderer, IWindow window) : base(logger, renderer, window)
    {
        UiContext = new ImGuiContext(renderer, window, window.CreateInput());
    }
    public override void BeforeDraw()
    {
        base.BeforeDraw();
        ImGui.NewFrame();
        _frameBegun = true;
    }
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        UiContext.Update(deltaTime);
    }

    protected override void SubmitUI(CommandBuffer cmd, float deltaTime)
    {
        base.SubmitUI(cmd, deltaTime);
        if (!_frameBegun) return;
        _frameBegun = false;
        
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(Window.Size.X, Window.Size.Y);
        if (Window.Size is {X: > 0, Y: > 0})
            io.DisplayFramebufferScale = new Vector2(Window.FramebufferSize.X / (float) Window.Size.X,
                Window.FramebufferSize.Y / (float) Window.Size.Y);
        
        UiContext.Render(cmd);

        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }
    }

    public override void Release()
    {
        Renderer.Device.WaitIdle();
        base.Release();
        UiContext.Dispose();
    }
}
