using Licht.GraphicsCore.Graphics;
using Licht.Vulkan.Pipelines;
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
    public void ImageMemoryBarrier(ImageMemoryBarrier barrierInfo, PipelineStageFlags srcStageFlags,
        PipelineStageFlags dstStageFlags) =>
        vk.CmdPipelineBarrier(this, srcStageFlags, dstStageFlags,
            0, 0, null, 0, null, 1, barrierInfo);

    public void CopyBufferToImage(VkBuffer buffer, VkImage image, ImageLayout layout, BufferImageCopy copyRegion) =>
        vk.CmdCopyBufferToImage(this, buffer, image, layout, 1, copyRegion);
    public void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, BufferCopy copyRegion) 
        => vk.CmdCopyBuffer(this, srcBuffer, dstBuffer, 1, &copyRegion);
    public void CopyImageToBuffer(VkImage image, VkBuffer buffer, ImageLayout layout, BufferImageCopy copyRegion) 
        => vk.CmdCopyImageToBuffer(this, image, layout, buffer, 1, copyRegion);
    public void BindGraphicsPipeline(GraphicsPipeline pipeline) =>
        vk.CmdBindPipeline(this, PipelineBindPoint.Graphics, pipeline);

    public void BindComputePipeline(GraphicsPipeline pipeline) =>
        vk.CmdBindPipeline(this, PipelineBindPoint.Compute, pipeline);

    public void BindGraphicsDescriptorSet(DescriptorSet set, ShaderEffect effect) =>
        vk.CmdBindDescriptorSets(this, PipelineBindPoint.Graphics, effect.EffectLayout, 0, 1, set, 0, null);

    public void BindGraphicsDescriptorSets(DescriptorSet[] sets, ShaderEffect effect)
    {
        fixed(DescriptorSet* pSets = sets)
            vk.CmdBindDescriptorSets(this, PipelineBindPoint.Graphics, effect.EffectLayout, 0, (uint)sets.Length, pSets, 0, null);
    }
    public void BindComputeDescriptorSet(DescriptorSet set, ShaderEffect effect) =>
        vk.CmdBindDescriptorSets(this, PipelineBindPoint.Compute, effect.EffectLayout, 0, 1, set, 0, null);

    public void BindComputeDescriptorSets(DescriptorSet[] sets, ShaderEffect effect)
    {
        fixed(DescriptorSet* pSets = sets)
            vk.CmdBindDescriptorSets(this, PipelineBindPoint.Compute, effect.EffectLayout, 0, (uint)sets.Length, pSets, 0, null);
    }


    public void BindVertexBuffer(Buffer vertexBuffer, ulong vertexOffset = 0) =>
        vk.CmdBindVertexBuffers(this, 0u, 1, vertexBuffer, &vertexOffset);
    public void BindIndexBuffer(Buffer indexBuffer, IndexType indexType) =>
        vk.CmdBindIndexBuffer(this, indexBuffer, 0, indexType);

    public void PushConstants(ShaderEffect effect, ShaderStageFlags flags, uint offset, uint scale, void* data) =>
        vk.CmdPushConstants(this, effect.EffectLayout, flags, offset, scale, data);
    public void Draw(uint vertexCount, uint instanceCount, uint firstVertex, uint firstInstance) =>
        vk.CmdDraw(this, vertexCount, instanceCount, firstVertex, firstInstance);
    public void DrawIndexed(uint indexCount, uint instanceCount, uint firstIndex, int vertexOffset, uint firstInstance) =>
        vk.CmdDrawIndexed(this, indexCount, instanceCount, firstIndex, vertexOffset, firstInstance);
    public void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ = 1) =>
        vk.CmdDispatch(this, groupCountX, groupCountY, groupCountZ);
}
