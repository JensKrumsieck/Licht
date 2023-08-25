using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Catalyst.Engine;
using Catalyst.Engine.Graphics;
using ImGuiNET;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;

using var app = new Application();
app.AttachLayer(new RayTracingLayer());
app.Run();

internal unsafe class RayTracingLayer : ILayer
{
    private GraphicsDevice _device = null!;
    private uint _viewportWidth;
    private uint _viewportHeight;
    private Texture? _texture;
    private uint[]? _imageData;
    
    private Random _random = new();
    private Stopwatch _renderTimer = new Stopwatch();

    private float _lastRenderTime = 0;
    
    public void OnAttach()
    {
        _device = Application.GetDevice();
    }

    public void OnDrawGui(double deltaTime)
    {
        ImGui.DockSpaceOverViewport();
        ImGui.Begin("Settings");
        ImGui.Text($"Last Render: {_lastRenderTime} ms");
        if (ImGui.Button("Render")) Render();
        ImGui.End();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.Begin("Viewport");
        _viewportWidth = (uint)ImGui.GetContentRegionAvail().X;
        _viewportHeight = (uint)ImGui.GetContentRegionAvail().Y;
        
        Render();
        
        if (_texture is not null)
            ImGui.Image((nint) _texture.Image.Handle, new Vector2(_viewportWidth, _viewportHeight));
        ImGui.End();
        ImGui.PopStyleVar();
        
    }
    private void Render()
    {
        _renderTimer.Start();
        if (_texture is null || _imageData is null || _viewportWidth != _texture.Width || _viewportHeight != _texture.Height)
        {
            _texture?.Dispose();
            _imageData = new uint[_viewportWidth * _viewportHeight];
            _texture = new Texture(_device, _viewportWidth, _viewportHeight, Format.R8G8B8A8Unorm, null);
            Application.GetApplication().LoadUITexture(_texture);
        }

        for (var i = 0; i < _viewportWidth * _viewportHeight; i++)
        {
            _imageData[i] = (uint) _random.Next(0, int.MaxValue);
            _imageData[i] |= 0xfff00000;
        }
            
        fixed(uint* pImageData = _imageData)
            _texture.SetData(pImageData);
        _renderTimer.Stop();
        _lastRenderTime = _renderTimer.ElapsedMilliseconds;
        _renderTimer.Reset();
    }

    public void OnDetach()
    {
        _texture?.Dispose();
    }
}
