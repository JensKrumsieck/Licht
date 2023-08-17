using Catalyst.Tools;
using Silk.NET.Vulkan;

namespace Catalyst;

public struct CommandBuffer : IConvertibleTo<Silk.NET.Vulkan.CommandBuffer>
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

   public void BindGraphicsPipeline(Silk.NET.Vulkan.Pipeline pipeline) => vk.CmdBindPipeline(VkCommandBuffer, PipelineBindPoint.Graphics, pipeline);
}