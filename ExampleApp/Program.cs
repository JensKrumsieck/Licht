
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using Catalyst.Pipeline;
using ImGuiNET;

using var app = new Application();
app.AttachLayer(new ExampleLayer());
app.Run();

internal class ExampleLayer : ILayer
{
    private float ANumber = 2f;

    private GraphicsDevice _device = null!;
    private Renderer _renderer = null!;
    private ShaderEffect _shaderEffect;
    private ShaderPass _shaderPass;
    
    public void OnAttach()
    {
        _device = Application.GetDevice();
        _renderer = Application.GetRenderer();
        _shaderEffect = ShaderEffect.BuildEffect(_device.Device, "./Shaders/triShader.vert.spv",
            "./Shaders/triShader.frag.spv", null);
        var passInfo = ShaderPassInfo.Default();
        _shaderPass = new ShaderPass(_device.Device, _shaderEffect, passInfo, default, _renderer.RenderPass);
    }

    public void OnUpdate(double deltaTime)
    {
        _renderer.CurrentCommandBuffer.BindGraphicsPipeline(_shaderPass);
        _renderer.CurrentCommandBuffer.Draw(3, 1, 0, 0);
        _renderer.Window.Title = $"ImGui reports {ANumber}";
    }
    
    public void OnDrawGui(double deltaTime)
    {
        ImGui.ShowDemoWindow();
        ImGui.Begin("Test Window");
        ImGui.Text("Hello World!");
        ImGui.SliderFloat("Titlebar", ref ANumber, 0, 100);
        ImGui.End();

    }

    public void OnDetach()
    {
        _shaderEffect.Dispose();
        _shaderPass.Dispose();
    }
}