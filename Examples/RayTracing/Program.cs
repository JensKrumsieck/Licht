using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using ImGuiNET;
using RayTracing;
using Silk.NET.Maths;

using var app = new Application();
app.AttachLayer(new RayTracingLayer());
app.Run();

internal class RayTracingLayer : ILayer
{
    private GraphicsDevice _device = null!;
    private RayTracer _renderer = null!;
    private Camera _camera = null!;
    private Scene _scene = null!;
    
    private uint _viewportWidth;
    private uint _viewportHeight;
    private readonly Stopwatch _renderTimer = new();

    private float _lastRenderTime;
    
    public void OnAttach()
    {
        _device = Application.GetDevice();
        _renderer = new RayTracer(_device);
        _camera = new Camera(45f, 0.1f, 100f);
        _scene = new Scene();
        _scene.Spheres.Add(new Sphere(Vector3.Zero, .5f, new Vector3(1,0,1)));
        _scene.Spheres.Add(new Sphere(new Vector3(1,0,-5), 1.5f, new Vector3(.2f,.3f,1)));
    }

    public void OnUpdate(double deltaTime)
    {
        _camera.OnUpdate((float) deltaTime);
    }

    public void OnDrawGui(double deltaTime)
    {
        ImGui.DockSpaceOverViewport();
        ImGui.Begin("Settings");
        ImGui.Text($"Last Render: {_lastRenderTime} ms");
        if (ImGui.Button("Render")) Render();
        ImGui.End();
        
        ImGui.Begin("Scene");
        for (var i = 0; i < _scene.Spheres.Count; i++)
        {
            ImGui.PushID(i);
            ImGui.DragFloat3("Position", ref _scene.Spheres[i].Position, 0.1f);
            ImGui.DragFloat("Radius", ref _scene.Spheres[i].Radius, .1f);
            ImGui.ColorEdit3("Color", ref _scene.Spheres[i].Albedo, ImGuiColorEditFlags.Float);
            ImGui.PopID();
        }
        ImGui.End();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.Begin("Viewport");
        
        _viewportWidth = (uint)ImGui.GetContentRegionAvail().X;
        _viewportHeight = (uint)ImGui.GetContentRegionAvail().Y;
        
        Render();

        var image = _renderer.FinalImage;
        ImGui.Image((nint) image.DescriptorSet.Handle, new Vector2(image.Width, image.Height), new Vector2(0,1), new Vector2(1,0));
        ImGui.End();
        ImGui.PopStyleVar();
        
    }
    private void Render()
    {
        _renderTimer.Start();
        
        _renderer.OnResize(_viewportWidth, _viewportHeight);
        _camera.OnResize(_viewportWidth, _viewportHeight);
        _renderer.Render(_scene, _camera);
        
        _renderTimer.Stop();
        _lastRenderTime = _renderTimer.ElapsedMilliseconds;
        _renderTimer.Reset();
    }

    public void OnDetach() => _renderer.Dispose();
}
