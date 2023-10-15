using System.Drawing;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using Licht.Vulkan.Pipelines;
using Silk.NET.Input;
using Silk.NET.Input.Extensions;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Licht.Vulkan.UI;

public unsafe class ImGuiContext : IDisposable
{
    private readonly VkRenderer _renderer;
    private readonly VkGraphicsDevice _device;
    private readonly IInputContext _input;
    private readonly IWindow _window;
    
    private readonly DescriptorPool _descriptorPool;
    private readonly DescriptorSetLayout _descriptorSetLayout;
    private readonly VkImage _fontTexture;
    private readonly PipelineEffect _effect;
    private readonly VkGraphicsPipeline _pipeline;
    private readonly Dictionary<IntPtr, VkImage> _loadedTextures = new ();
    
    private VkBuffer?[]? _vertexBuffers;
    private VkBuffer?[]? _indexBuffers;
    
    private readonly IKeyboard _keyboard;
    private readonly List<char> _pressedChars = new();
    private Key[]? _allKeys;

    public ImGuiContext(VkRenderer renderer, IWindow window, IInputContext input)
    {
        _renderer = renderer;
        _device = _renderer.Device;
        _input = input;
        _window = window;
        
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        
        io.Fonts.AddFontDefault();
        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out var width, out var height);
        _descriptorPool = _renderer.Device.CreateDescriptorPool(new[] {new DescriptorPoolSize(DescriptorType.CombinedImageSampler, 1000)});
        _fontTexture = new VkImage(_renderer.Device, (uint) width, (uint) height, Format.R8G8B8A8Unorm, ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit, pixels.ToPointer());
        var sampler = _fontTexture.ImageInfo.Sampler;
        var binding0 = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            StageFlags = ShaderStageFlags.FragmentBit,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            PImmutableSamplers = &sampler
        };
        _descriptorSetLayout = _device.CreateDescriptorSetLayout(binding0);
        _fontTexture.PrepareBind(_descriptorPool, _descriptorSetLayout);
        io.Fonts.SetTexID((nint)_fontTexture.Image.Handle);
        
        var pushRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.VertexBit,
            Offset = 0,
            Size = sizeof(float) * 4
        };
        _effect = PipelineEffect.BuildEffect(_device, UIShaders.VertexShader, UIShaders.FragmentShader, new[] {_descriptorSetLayout}, pushRange);
        var vertexInfo = new VertexInfo(
            new VertexInputBindingDescription[]
            {
                new(0, (uint) Unsafe.SizeOf<ImDrawVert>(), VertexInputRate.Vertex)
            },
            new VertexInputAttributeDescription[]
            {
                new(0, 0, Format.R32G32Sfloat, (uint) Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.pos))),
                new(1, 0, Format.R32G32Sfloat, (uint) Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.uv))),
                new(2, 0, Format.R8G8B8A8Unorm, (uint) Marshal.OffsetOf<ImDrawVert>(nameof(ImDrawVert.col)))
            });
        var info = GraphicsPipelineDescription.Default();
        info.EnableAlphaBlend();
        info.NoDepthTesting();
        _pipeline = new VkGraphicsPipeline(_device, _effect, info, vertexInfo, _renderer.RenderPass!.Value);
        SetKeyMappings();
        SetFrameData();
        _keyboard = _input.Keyboards[0];
        _keyboard.KeyChar += OnKeyChar;
    }
    
    private void SetKeyMappings()
    {
        var io = ImGui.GetIO();
        io.KeyMap[(int)ImGuiKey.Tab] = (int)Key.Tab;
        io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Key.Left;
        io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Key.Right;
        io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Key.Up;
        io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Key.Down;
        io.KeyMap[(int)ImGuiKey.PageUp] = (int)Key.PageUp;
        io.KeyMap[(int)ImGuiKey.PageDown] = (int)Key.PageDown;
        io.KeyMap[(int)ImGuiKey.Home] = (int)Key.Home;
        io.KeyMap[(int)ImGuiKey.End] = (int)Key.End;
        io.KeyMap[(int)ImGuiKey.Delete] = (int)Key.Delete;
        io.KeyMap[(int)ImGuiKey.Backspace] = (int)Key.Backspace;
        io.KeyMap[(int)ImGuiKey.Enter] = (int)Key.Enter;
        io.KeyMap[(int)ImGuiKey.Escape] = (int)Key.Escape;
        io.KeyMap[(int)ImGuiKey.A] = (int)Key.A;
        io.KeyMap[(int)ImGuiKey.C] = (int)Key.C;
        io.KeyMap[(int)ImGuiKey.V] = (int)Key.V;
        io.KeyMap[(int)ImGuiKey.X] = (int)Key.X;
        io.KeyMap[(int)ImGuiKey.Y] = (int)Key.Y;
        io.KeyMap[(int)ImGuiKey.Z] = (int)Key.Z;
    }
    
    private void OnKeyChar(IKeyboard kb, char @char) => _pressedChars.Add(@char);
    
    private void SetFrameData(float deltaTime = 1f/60f)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_window.Size.X, _window.Size.Y);
        if (_window.Size is {X: > 0, Y: > 0})
            io.DisplayFramebufferScale = new Vector2(_window.FramebufferSize.X / (float) _window.Size.X,
                _window.FramebufferSize.Y / (float) _window.Size.Y);
        io.DeltaTime = deltaTime;
    }
    
    public void AddTexture(VkImage texture)
    {
        texture.PrepareBind(_descriptorPool, _descriptorSetLayout);
        _loadedTextures[(nint) texture.ImGuiDescriptorSet.Handle] = texture;
    }
    public void RemoveTexture(VkImage texture)
    {
        _loadedTextures.Remove((nint)texture.ImGuiDescriptorSet.Handle);
        texture.Free(_descriptorPool);
    }
    
    private void UpdateImGuiInput()
    {
        var io = ImGui.GetIO();

        var mouseState = _input.Mice[0].CaptureState();
        var keyboardState = _input.Keyboards[0];

        io.MouseDown[0] = mouseState.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouseState.IsButtonPressed(MouseButton.Right);
        io.MouseDown[2] = mouseState.IsButtonPressed(MouseButton.Middle);

        var point = new Point((int)mouseState.Position.X, (int)mouseState.Position.Y);
        io.MousePos = new Vector2(point.X, point.Y);

        var wheel = mouseState.GetScrollWheels()[0];
        io.MouseWheel = wheel.Y;
        io.MouseWheelH = wheel.X;

        _allKeys ??= (Key[]?) Enum.GetValues(typeof(Key));
        foreach (var key in _allKeys!)
        {
            if (key == Key.Unknown) continue;
            io.KeysDown[(int)key] = keyboardState.IsKeyPressed(key);
        }

        foreach (var c in _pressedChars) io.AddInputCharacter(c);
        _pressedChars.Clear();

        io.KeyCtrl = keyboardState.IsKeyPressed(Key.ControlLeft) || keyboardState.IsKeyPressed(Key.ControlRight);
        io.KeyAlt = keyboardState.IsKeyPressed(Key.AltLeft) || keyboardState.IsKeyPressed(Key.AltRight);
        io.KeyShift = keyboardState.IsKeyPressed(Key.ShiftLeft) || keyboardState.IsKeyPressed(Key.ShiftRight);
        io.KeySuper = keyboardState.IsKeyPressed(Key.SuperLeft) || keyboardState.IsKeyPressed(Key.SuperRight);
    }

    public void Update(float deltaTime) => UpdateImGuiInput();
    
    public void Render(CommandBuffer cmd)
    {
        ImGui.Render();
        RenderDrawData(cmd, ImGui.GetDrawData());
    }
    
     private void RenderDrawData(CommandBuffer cmd, in ImDrawDataPtr drawDataPtr)
    {
        var framebufferWidth = (int) drawDataPtr.DisplaySize.X * drawDataPtr.FramebufferScale.X;
        var frameBufferHeight = (int) drawDataPtr.DisplaySize.Y * drawDataPtr.FramebufferScale.Y;
        if (framebufferWidth <= 0 || frameBufferHeight <= 0) return;

        var drawData = *drawDataPtr.NativePtr;
        if (_vertexBuffers is null || _indexBuffers is null)
        {
            _vertexBuffers = new VkBuffer[_renderer.Swapchain!.ImageCount];
            _indexBuffers = new VkBuffer[_renderer.Swapchain!.ImageCount];
        }

        ref var vertexBuffer = ref _vertexBuffers[_renderer.CurrentImageIndex];
        ref var indexBuffer = ref _indexBuffers[_renderer.CurrentImageIndex];

        if (drawData.TotalVtxCount > 0)
        {
            var vertexSize = (ulong) drawData.TotalVtxCount * (ulong) sizeof(ImDrawVert);
            var indexSize = (ulong) drawData.TotalIdxCount * sizeof(ushort);
            if (vertexBuffer is null || vertexBuffer.BufferSize < vertexSize)
                CreateOrResizeBuffer(ref vertexBuffer, (uint) drawData.TotalVtxCount, (uint) sizeof(ImDrawVert),
                                     BufferUsageFlags.VertexBufferBit);
            if (indexBuffer is null || indexBuffer.BufferSize < indexSize)
                CreateOrResizeBuffer(ref indexBuffer, (uint) drawData.TotalIdxCount, sizeof(ushort),
                                     BufferUsageFlags.IndexBufferBit);
            var pVtx = IntPtr.Zero.ToPointer();
            var pIdx = IntPtr.Zero.ToPointer();
            vertexBuffer?.Map(ref pVtx);
            indexBuffer?.Map(ref pIdx);
            ulong vtxOffset = 0;
            ulong idxOffset = 0;
            for (var n = 0; n < drawData.CmdListsCount; n++)
            {
                ref var cmdList = ref drawData.CmdLists[n];
                ulong vtxChunkSize = (uint) cmdList->VtxBuffer.Size * (uint) sizeof(ImDrawVert);
                ulong idxChunkSize = (uint) cmdList->IdxBuffer.Size * sizeof(ushort);
                vertexBuffer?.WriteToBuffer(cmdList->VtxBuffer.Data.ToPointer(), pVtx, vtxChunkSize, vtxOffset);
                indexBuffer?.WriteToBuffer(cmdList->IdxBuffer.Data.ToPointer(), pIdx, idxChunkSize, idxOffset);
                vtxOffset += vtxChunkSize;
                idxOffset += idxChunkSize;
            }

            vertexBuffer!.Flush();
            indexBuffer!.Flush();
            vertexBuffer!.Unmap();
            indexBuffer!.Unmap();
        }

        cmd.BindGraphicsPipeline(_pipeline); 
        _fontTexture.Bind(cmd, _effect);
        
        if (drawData.TotalVtxCount > 0)
        {
            cmd.BindVertexBuffer(vertexBuffer!.Buffer);
            cmd.BindIndexBuffer(indexBuffer!.Buffer, IndexType.Uint16);
        }

        //imGui viewport & scissor
        var viewport = new Viewport(0, 0, framebufferWidth, frameBufferHeight, 0, 1);
        cmd.SetViewport(viewport);
        var scale = stackalloc float[2];
        scale[0] = 2.0f / drawData.DisplaySize.X;
        scale[1] = 2.0f / drawData.DisplaySize.Y;
        var translate = stackalloc float[2];
        translate[0] = -1.0f - drawData.DisplayPos.X * scale[0];
        translate[1] = -1.0f - drawData.DisplayPos.Y * scale[1];

        cmd.PushConstants(_effect, ShaderStageFlags.VertexBit, sizeof(float) * 0, sizeof(float) * 2, scale);
        cmd.PushConstants(_effect, ShaderStageFlags.VertexBit, sizeof(float) * 2, sizeof(float) * 2, translate);

        var clipOff = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;

        var vertexOffset = 0;
        var indexOffset = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            ref var cmdList = ref drawData.CmdLists[n];
            for (var i = 0; i < cmdList->CmdBuffer.Size; i++)
            {
                ref var pCmd = ref cmdList->CmdBuffer.Ref<ImDrawCmd>(i);
                if (pCmd.TextureId != IntPtr.Zero)
                {
                    if (pCmd.TextureId == (nint)_fontTexture.Image.Handle)
                        _fontTexture.Bind(cmd, _effect);
                    else
                        _loadedTextures[pCmd.TextureId].Bind(cmd, _effect);
                }

                var clipRect = new Vector4
                {
                    X = (pCmd.ClipRect.X - clipOff.X) * clipScale.X,
                    Y = (pCmd.ClipRect.Y - clipOff.Y) * clipScale.Y,
                    Z = (pCmd.ClipRect.Z - clipOff.X) * clipScale.X,
                    W = (pCmd.ClipRect.W - clipOff.Y) * clipScale.Y,
                };
                if (clipRect.X < framebufferWidth
                    && clipRect.Y < frameBufferHeight
                    && clipRect.Z >= .0f
                    && clipRect.W >= .0f)
                {
                    if (clipRect.X < 0.0f) clipRect.X = 0.0f;
                    if (clipRect.Y < 0.0f) clipRect.Y = 0.0f;


                    var scissor = new Rect2D(
                                             new Offset2D((int) clipRect.X, (int) clipRect.Y),
                                             new Extent2D((uint) (clipRect.Z - clipRect.X),
                                                          (uint) (clipRect.W - clipRect.Y))
                                            );
                    cmd.SetScissor(scissor);
                    cmd.DrawIndexed(pCmd.ElemCount, 1, pCmd.IdxOffset + (uint) indexOffset,
                                    (int) pCmd.VtxOffset + vertexOffset, 0);

                }
            }
            indexOffset += cmdList->IdxBuffer.Size;
            vertexOffset += cmdList->VtxBuffer.Size;
        }
    }

    private void CreateOrResizeBuffer(ref VkBuffer? buffer, uint instanceSize, uint instanceCount, BufferUsageFlags usage)
    {
        buffer?.Dispose();
        buffer = new VkBuffer(_device, instanceSize * instanceCount, usage, MemoryPropertyFlags.HostVisibleBit);
    }
    
    public void Dispose()
    {
        _keyboard.KeyChar -= OnKeyChar;
        _fontTexture.Free(_descriptorPool);
        _fontTexture.Dispose();
        foreach (var t in _loadedTextures.Values) RemoveTexture(t);
        
        _pipeline.Dispose();
        _effect.Dispose();
        _descriptorSetLayout.Dispose();
        _descriptorPool.Dispose();
        
        if (_indexBuffers is not null && _vertexBuffers is not null)
        {
            for (var i = 0; i < _vertexBuffers?.Length; i++)
            {
                _vertexBuffers[i]?.Dispose();
                _indexBuffers![i]?.Dispose();
            }
            Array.Clear(_vertexBuffers!);
            Array.Clear(_indexBuffers!);
        }
        
        GC.SuppressFinalize(this);
    }
}
