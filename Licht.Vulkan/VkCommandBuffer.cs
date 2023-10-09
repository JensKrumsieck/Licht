using Licht.GraphicsCore.Graphics;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public unsafe class VkCommandBuffer : ICommandList
{
    private readonly CommandBuffer _commandBuffer;

    internal VkCommandBuffer(CommandBuffer cmd) => _commandBuffer = cmd;
    
    public static implicit operator CommandBuffer(VkCommandBuffer cmd) => cmd._commandBuffer;
    public Result Begin()
    {
        var beginInfo = new CommandBufferBeginInfo {SType = StructureType.CommandBufferBeginInfo};
        return vk.BeginCommandBuffer(this, beginInfo);
    }

    public Result End() => vk.EndCommandBuffer(this);
    public void BeginRenderPass(RenderPassBeginInfo beginInfo) => vk.CmdBeginRenderPass(this, beginInfo, SubpassContents.Inline);

    public void EndRenderPass() => vk.CmdEndRenderPass(this);

    public void SetViewport(Viewport viewport) => vk.CmdSetViewport(this, 0, 1, viewport);
    public void SetScissor(Rect2D scissor) => vk.CmdSetScissor(this, 0, 1, scissor);
}
