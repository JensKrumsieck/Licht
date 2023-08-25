using System.Security.Cryptography;
using Catalyst.Pipeline;
using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst;

public unsafe struct CommandBuffer : IConvertibleTo<Silk.NET.Vulkan.CommandBuffer>
{
    public Silk.NET.Vulkan.CommandBuffer VkCommandBuffer;

    public static implicit operator Silk.NET.Vulkan.CommandBuffer(CommandBuffer cmd) => cmd.VkCommandBuffer;

    public Silk.NET.Vulkan.CommandBuffer Convert() => VkCommandBuffer;

    public Result Begin()
    {
        var beginInfo = new CommandBufferBeginInfo {SType = StructureType.CommandBufferBeginInfo};
        return vk.BeginCommandBuffer(this, beginInfo);
    }

    public Result End() => vk.EndCommandBuffer(this);

    public void BeginRenderPass(RenderPassBeginInfo beginInfo) =>
        vk.CmdBeginRenderPass(this, beginInfo, SubpassContents.Inline);

    public void EndRenderPass() => vk.CmdEndRenderPass(this);

    public void SetViewport(Viewport viewport) => vk.CmdSetViewport(this, 0, 1, viewport);
    public void SetScissor(Rect2D scissor) => vk.CmdSetScissor(this, 0, 1, scissor);

    public unsafe void ImageMemoryBarrier(ImageMemoryBarrier barrierInfo, PipelineStageFlags srcStageFlags,
                                          PipelineStageFlags dstStageFlags) =>
        vk.CmdPipelineBarrier(this, srcStageFlags, dstStageFlags,
                              0, 0, null, 0, null, 1, barrierInfo);

    public void CopyBufferToImage(Buffer buffer, Image image, ImageLayout layout, BufferImageCopy copyRegion) =>
        vk.CmdCopyBufferToImage(this, buffer, image, layout, 1, copyRegion);

    public void BindGraphicsPipeline(Silk.NET.Vulkan.Pipeline pipeline) =>
        vk.CmdBindPipeline(VkCommandBuffer, PipelineBindPoint.Graphics, pipeline);

    public unsafe void BindGraphicsDescriptorSet(DescriptorSet set, ShaderEffect effect) =>
        vk.CmdBindDescriptorSets(this, PipelineBindPoint.Graphics, effect.EffectLayout, 0, 1, set, 0, null);

    public unsafe void BindGraphicsDescriptorSets(DescriptorSet[] sets, ShaderEffect effect)
    {
        fixed(Silk.NET.Vulkan.DescriptorSet* pSets = sets.AsArray<DescriptorSet, Silk.NET.Vulkan.DescriptorSet>())
            vk.CmdBindDescriptorSets(this, PipelineBindPoint.Graphics, effect.EffectLayout, 0, 1, pSets, 0, null);
    }


    public void BindVertexBuffer(Buffer vertexBuffer, ulong vertexOffset = 0) =>
        vk.CmdBindVertexBuffers(this, 0u, 1, &vertexBuffer.VkBuffer, &vertexOffset);
    public void BindIndexBuffer(Buffer indexBuffer, IndexType indexType) =>
        vk.CmdBindIndexBuffer(this, indexBuffer.VkBuffer, 0, indexType);

    public void PushConstants(ShaderEffect effect, ShaderStageFlags flags, uint offset, uint scale, void* data) =>
        vk.CmdPushConstants(this, effect.EffectLayout, flags, offset, scale, data);

    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance) =>
        vk.CmdDraw(this, vertexCount, instanceCount, firstVertex, firstInstance);
    public void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance) =>
        vk.CmdDrawIndexed(this, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
}