using System.Numerics;
using Catalyst.Applications;
using ImGuiNET;
using Silk.NET.Input;

namespace Catalyst.UI;

public class ImGuiRenderer : IAppModule
{
    private readonly ImGuiContext _guiContext;
    private readonly Renderer _renderer;
    private readonly IInputContext _input;
    
    private bool _frameBegun;
    public ImGuiRenderer( Renderer renderer, IInputContext input)
    {
        _renderer = renderer;
        _input = input;
        _guiContext = new ImGuiContext(_renderer, _input);
        
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        ImGui.StyleColorsDark();
    }
    
    public void Dispose() => _guiContext.Dispose();
    
    public void OnUpdate(double deltaTime)=> _guiContext.Update((float)deltaTime);
    
    public void Begin()
    {
        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void End()
    {
        if (!_frameBegun) return;
        _frameBegun = false;
        
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
        
    public void LoadTexture(Texture t) => _guiContext.AddTexture(t);
    public void UnloadTexture(Texture t) => _guiContext.RemoveTexture(t);
}
