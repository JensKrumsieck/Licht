
using Catalyst.Engine;
using ImGuiNET;

using var app = new Application();
app.AttachLayer(new ExampleLayer());
app.Run();
internal class ExampleLayer : ILayer
{
    public void OnUpdate(double deltaTime)
    {
        //Console.WriteLine("Update " + deltaTime);
    }
    public void OnDrawGui(double deltaTime)
    {
        ImGui.ShowDemoWindow();
    }
}