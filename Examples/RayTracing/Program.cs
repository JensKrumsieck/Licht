using System.Diagnostics;
using System.Numerics;
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using ImGuiNET;
using RayTracing;
using RayTracing.Shapes;
using Plane = RayTracing.Shapes.Plane;
using Renderer = RayTracing.Renderer;

using var app = new Application();

var styleSettings = new StyleSettings();
styleSettings.Set();

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
            Roughness = .5f
        });
        _scene.Materials.Add(new Material
        {
            Albedo = new Vector3(.8f, .5f, .2f),
            Roughness = 1f,
            EmissionPower = 20,
            EmissionColor = new Vector3(.8f, .5f, .2f)
        });
        _scene.Materials.Add(new Material
        {
            Albedo = new Vector3(.9f, .1f, .1f),
            Roughness = 1f
        });
        _scene.Objects.Add(
            new Sphere
            {
                Position = Vector3.Zero,
                Radius = 1f,
                MaterialIndex = 0
            });
        _scene.Objects.Add(
            new Sphere
            {
                Position = Vector3.UnitX * 3,
                Radius = 1f,
                MaterialIndex = 1
            });
        _scene.Objects.Add(
            new Sphere
            {
                Position = Vector3.UnitX * -2,
                Radius = .5f,
                MaterialIndex = 3
            });
        _scene.Objects.Add(
            new Sphere
            {
                Position = Vector3.UnitY * 2,
                Radius = 1f,
                MaterialIndex = 2
            });
        _scene.Objects.Add(
            new Plane
            {
                Position = -Vector3.UnitY,
                MaterialIndex = 1
            });
    }

    public void OnUpdate(double deltaTime)
    {
        if(_camera.OnUpdate((float) deltaTime)) 
            _renderer.ResetFrameIndex();
    }

    public void OnDrawGui(double deltaTime)
    {
        ImGui.DockSpaceOverViewport();
        ImGui.Begin("Settings");
        ImGui.Text($"Last Render: {_lastRenderTime} ms");
        ImGui.Checkbox("Accumulate", ref _renderer.Settings.Accumulate);
        ImGui.SeparatorText("Blur Settings");
        ImGui.Checkbox("Enable Blur", ref _renderer.Settings.Blur);
        if(_renderer.Settings.Blur){
            ImGui.DragFloat("Blur Sigma", ref _renderer.Settings.BlurSettings.Sigma, .01f, 0, float.MaxValue);
            ImGui.DragInt("Blur Kernel", ref _renderer.Settings.BlurSettings.KernelSize, 1, 0, int.MaxValue);
        }
        ImGui.SeparatorText("World Settings");
        ImGui.Checkbox("Use World", ref _renderer.Settings.UseWorld);
        if (_renderer.Settings.UseWorld)
            ImGui.ColorEdit3("World Color", ref _renderer.Settings.WorldColor, ImGuiColorEditFlags.Float);
        ImGui.Separator();
        if(ImGui.Button("Reset")) _renderer.ResetFrameIndex();
        ImGui.End();
        
        ImGui.Begin("Scene");
        for (var i = 0; i < _scene.Objects.Count; i++)
        {
            ImGui.Text($"{_scene.Objects[i].GetType().Name} with Index {i}");
            ImGui.DragFloat3("Position", ref _scene.Objects[i].Position, 0.1f);
            if(_scene.Objects[i] is Sphere sphere)
                ImGui.DragFloat("Radius", ref sphere.Radius, .1f);
            ImGui.DragInt("Material Index", ref _scene.Objects[i].MaterialIndex, 1, 0, _scene.Materials.Count - 1);
            ImGui.Separator();
            ImGui.PopID();
        }
        ImGui.End();
        ImGui.Begin("Materials");
        for (var i = 0; i < _scene.Materials.Count; i++)
        {
            ImGui.PushID(i);
            ImGui.Text($"Material {i}");
            ImGui.ColorEdit3("Base Color", ref _scene.Materials[i].Albedo, ImGuiColorEditFlags.Float);
            ImGui.SliderFloat("Metallic", ref _scene.Materials[i].Metallic, 0, 1);
            ImGui.SliderFloat("Roughness", ref _scene.Materials[i].Roughness, 0, 1);
            ImGui.SliderFloat("Specular", ref _scene.Materials[i].Specular, 0,  1);
            ImGui.ColorEdit3("Emission Color", ref _scene.Materials[i].EmissionColor, ImGuiColorEditFlags.Float);
            ImGui.DragFloat("Emission Power", ref _scene.Materials[i].EmissionPower, 0.01f, 0, float.MaxValue);
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
