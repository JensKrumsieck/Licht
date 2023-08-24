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
    
    public void OnAttach()
    {
        _device = Application.GetDevice();
    }

    public void OnDrawGui(double deltaTime)
    {
        ImGui.DockSpaceOverViewport();
        ImGui.Begin("Settings");
        if (ImGui.Button("Render")) Render();
        ImGui.End();

        ImGui.Begin("Viewport");
        _viewportWidth = (uint)ImGui.GetContentRegionAvail().X;
        _viewportHeight = (uint)ImGui.GetContentRegionAvail().Y;
        if (_texture is not null)
        {
            ImGui.Image(1 ,new Vector2(_viewportWidth, _viewportHeight));
        }
        ImGui.End();
    }
    private void Render()
    {
        if (_texture is null || _imageData is null || _viewportWidth != _texture.Width || _viewportHeight != _texture.Height)
        {
            _texture?.Dispose();
            _imageData = new uint[_viewportWidth * _viewportHeight];
            _texture = new Texture(_device, _viewportWidth, _viewportHeight, Format.R8G8B8A8Unorm, null);
            
        }
        _texture?.BindAsUIImage();

        for (var i = 0; i < _viewportWidth * _viewportHeight; i++) 
            _imageData[i] = 0xffff00ff;
        
        fixed(uint* pImageData = _imageData)
            _texture.SetData(pImageData);
    }

    public void OnDetach()
    {
        _texture?.Dispose();
    }
}
