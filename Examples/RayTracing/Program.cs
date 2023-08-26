using System.Diagnostics;
using System.Numerics;
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using ImGuiNET;
using RayTracing;
using Renderer = RayTracing.Renderer;

using var app = new Application();
app.AttachLayer(new RayTracingLayer());
app.Run();

internal class RayTracingLayer : ILayer
{
    private GraphicsDevice _device = null!;
    private Renderer _renderer = null!;
    private Camera _camera = null!;
    private Scene _scene = null!;
    
    private uint _viewportWidth;
    private uint _viewportHeight;
    private readonly Stopwatch _renderTimer = new();

    private float _lastRenderTime;
    
    public void OnAttach()
    {
        _device = Application.GetDevice();
        _renderer = new Renderer(_device);
        _camera = new Camera(45f, 0.1f, 100f);
        _scene = new Scene();
        _scene.Materials.Add(new Material
        {
            Albedo = new Vector3(1, 0, 1),
            Roughness = 0
        });
        _scene.Materials.Add(new Material
        {
            Albedo = new Vector3(.2f, .3f, 1),
            Roughness = .1f
        });
        _scene.Spheres.Add(
            new Sphere
            {
                Position = Vector3.Zero,
                Radius = 1f,
                MaterialIndex = 0
            });
        _scene.Spheres.Add(new Sphere
        {
            Position = Vector3.UnitY * -101,
            Radius = 100f,
            MaterialIndex = 1
        });
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
        ImGui.Checkbox("Accumulate", ref _renderer.Settings.Accumulate);
        if(ImGui.Button("Reset")) _renderer.ResetFrameIndex();
        ImGui.End();
        
        ImGui.Begin("Scene");
        ImGui.Text($"Spheres");
        for (var i = 0; i < _scene.Spheres.Count; i++)
        {
            ImGui.Text($"Sphere {i}");
            ImGui.DragFloat3("Position", ref _scene.Spheres[i].Position, 0.1f);
            ImGui.DragFloat("Radius", ref _scene.Spheres[i].Radius, .1f);
            ImGui.DragInt("Material Index", ref _scene.Spheres[i].MaterialIndex, 1, 0, _scene.Materials.Count);
            ImGui.Separator();
            ImGui.PopID();
        }
        
        ImGui.Text($"Materials");
        for (var i = 0; i < _scene.Materials.Count; i++)
        {
            ImGui.PushID(i);
            ImGui.Text($"Material {i}");
            ImGui.ColorEdit3("Color", ref _scene.Materials[i].Albedo, ImGuiColorEditFlags.Float);
            ImGui.DragFloat("Roughness", ref _scene.Materials[i].Roughness, 0.01f, 0, 1);
            ImGui.DragFloat("Metallic", ref _scene.Materials[i].Metallic, 0.01f, 0, 1);
            ImGui.Separator();
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
