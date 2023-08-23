using System.Numerics;
using Catalyst.Engine.Graphics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Catalyst.Engine.UI;

public class ImGuiLayer : ILayer
{
    internal ImGuiContext? _guiContext;
    private Renderer? _renderer;
    private IInputContext? _input;
    
    public void OnAttach()
    {
        _renderer = Application.GetRenderer();
        _input = Application.GetInput();
        _guiContext = new ImGuiContext(_renderer, _input);
        
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        // io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        // io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        // io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        ImGui.StyleColorsDark();
    }

    public void OnDetach() => _guiContext?.Dispose();

    public void OnUpdate(double deltaTime) => _guiContext?.Update((float)deltaTime);

    public void Begin() => ImGui.NewFrame();

    public void End()
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_renderer!.Window.Size.X, _renderer.Window.Size.Y);
        if (_renderer.Window.Size is {X: > 0, Y: > 0})
            io.DisplayFramebufferScale = new Vector2(_renderer.Window.FramebufferSize.X / (float) _renderer.Window.Size.X,
                _renderer.Window.FramebufferSize.Y / (float) _renderer.Window.Size.Y);
        
        _guiContext?.Render(_renderer.CurrentCommandBuffer);

        if (io.ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }
    }
}
