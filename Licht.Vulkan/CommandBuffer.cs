using Licht.Vulkan.Pipelines;
using Silk.NET.Vulkan;

namespace Licht.Vulkan;

public unsafe partial struct CommandBuffer
{
    public Result Begin()
    {
        var beginInfo = new CommandBufferBeginInfo {SType = StructureType.CommandBufferBeginInfo};
        return vk.BeginCommandBuffer(this, beginInfo);
    }
    public Result End() => vk.EndCommandBuffer(this);
    public void BeginRenderPass(RenderPassBeginInfo beginInfo) => BeginRenderPass(beginInfo, SubpassContents.Inline);


    public void SetViewport(Viewport viewport) => SetViewport(0, 1, viewport);
    public void SetScissor(Rect2D scissor) => SetScissor(0, 1, scissor);
    public void ImageMemoryBarrier(ImageMemoryBarrier barrierInfo, PipelineStageFlags srcStageFlags,PipelineStageFlags dstStageFlags) 
        => PipelineBarrier(srcStageFlags, dstStageFlags,
            0, 0, null, 0, null, 1, barrierInfo);

    public void CopyBufferToImage(VkBuffer buffer, VkImage image, ImageLayout layout, BufferImageCopy copyRegion) 
        => CopyBufferToImage(buffer, image, layout, 1, &copyRegion);
    public void CopyBuffer(VkBuffer srcBuffer, VkBuffer dstBuffer, BufferCopy copyRegion) 
        => CopyBuffer(srcBuffer, dstBuffer, 1, &copyRegion);
    public void CopyImageToBuffer(VkImage image, VkBuffer buffer, ImageLayout layout, BufferImageCopy copyRegion) 
        => CopyImageToBuffer(image, layout, buffer, 1, copyRegion);
    
    public void BindGraphicsPipeline(VkGraphicsPipeline pipeline)
        => BindPipeline(PipelineBindPoint.Graphics, pipeline);

    public void BindGraphicsDescriptorSet(DescriptorSet set, ShaderEffect effect) 
        => BindDescriptorSets(PipelineBindPoint.Graphics, effect.EffectLayout, 0, 1, set, 0, null);

    public void BindGraphicsDescriptorSets(DescriptorSet[] sets, ShaderEffect effect)
    {
        fixed(DescriptorSet* pSets = sets)
            BindDescriptorSets(PipelineBindPoint.Graphics, effect.EffectLayout, 0, (uint)sets.Length, pSets, 0, null);
    }
    public void BindComputeDescriptorSet(DescriptorSet set, ShaderEffect effect) =>
        BindDescriptorSets(PipelineBindPoint.Compute, effect.EffectLayout, 0, 1, set, 0, null);

    public void BindComputeDescriptorSets(DescriptorSet[] sets, ShaderEffect effect)
    {
        fixed(DescriptorSet* pSets = sets)
            BindDescriptorSets(PipelineBindPoint.Compute, effect.EffectLayout, 0, (uint)sets.Length, pSets, 0, null);
    }
    public void BindVertexBuffer(Buffer vertexBuffer, ulong vertexOffset = 0) =>
        BindVertexBuffers(0u, 1, vertexBuffer, &vertexOffset);
    public void BindIndexBuffer(Buffer indexBuffer, IndexType indexType) =>
        BindIndexBuffer(indexBuffer, 0, indexType);

    public void PushConstants(ShaderEffect effect, ShaderStageFlags flags, uint offset, uint scale, void* data) =>
        PushConstants(effect.EffectLayout, flags, offset, scale, data);
}
