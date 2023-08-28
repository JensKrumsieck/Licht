using Catalyst.Allocation;
using Catalyst.Tools;
using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;

namespace Catalyst.Engine.Graphics;

public sealed class GraphicsDevice : IDisposable
{
    private readonly Instance _instance;
    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private readonly CommandPool _commandPool;
    private readonly Surface _surface;
    private readonly IAllocator _allocator;

    public Device Device => _device;
    public Surface Surface => _surface;
    public IAllocator Allocator => _allocator;
    public CommandPool CommandPool => _commandPool;

    public GraphicsDevice(IVkSurfaceSource window)
    {
        _instance = new Instance();
        _physicalDevice = _instance.SelectPhysicalDevice(SelectorTools.DefaultGpuSelector);
        _device = new Device(_physicalDevice);
        _commandPool = new CommandPool(_device);
        _surface = new Surface(window, _instance);
        _allocator = new PassthroughAllocator(_device);
    }

    public SwapchainSupport GetSwapchainSupport() => _surface.GetSwapchainSupport(_physicalDevice);
    public void WaitIdle() => _device.WaitIdle();

    public CommandBuffer[] AllocateCommandBuffers(uint count) => _device.AllocateCommandBuffers(count, _commandPool);
    public void FreeCommandBuffers(CommandBuffer[] commandBuffers) => _device.FreeCommandBuffers(commandBuffers, _commandPool);

    public Sampler CreateSampler(SamplerCreateInfo info) => _device.CreateSampler(info);
    public void DestroySampler(Sampler sampler) => _device.DestroySampler(sampler);

    public AllocatedImage CreateImage(ImageCreateInfo info, MemoryPropertyFlags propertyFlags) =>
        _device.CreateImage(Allocator, info, propertyFlags);

    public void DestroyImage(AllocatedImage image) => _device.DestroyImage(image);

    public ImageView CreateImageView(ImageViewCreateInfo createInfo) => _device.CreateImageView(createInfo);
    public void DestroyImageView(ImageView view) => _device.DestroyImageView(view);

    public Buffer CreateBuffer(uint instanceSize, uint instanceCount, BufferUsageFlags usageFlags,
                               MemoryPropertyFlags memoryFlags) =>
        _device.CreateBuffer(_allocator, instanceSize, instanceCount, usageFlags, memoryFlags);

    public void TransitionImageLayout(Image image, Format format, ImageLayout oldLayout, ImageLayout newLayout,
                                      uint mipLevels, uint layerCount)
    {
        var cmd = BeginSingleTimeCommands();
        var range = new ImageSubresourceRange(null, 0, mipLevels, 0, layerCount);
        if (newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            range.AspectMask = ImageAspectFlags.DepthBit;
            if (format is Format.D32SfloatS8Uint or Format.D24UnormS8Uint)
                range.AspectMask |= ImageAspectFlags.StencilBit;
        }
        else range.AspectMask = ImageAspectFlags.ColorBit;

        var barrierInfo = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            Image = image,
            SubresourceRange = range,
        };
        
        PipelineStageFlags srcStage;
        PipelineStageFlags dstStage;
        if (oldLayout == ImageLayout.Undefined && newLayout is ImageLayout.TransferDstOptimal or ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.SrcAccessMask = 0;
            barrierInfo.DstAccessMask = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TopOfPipeBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            barrierInfo.SrcAccessMask = 0;
            barrierInfo.DstAccessMask =
                AccessFlags.DepthStencilAttachmentReadBit | AccessFlags.DepthStencilAttachmentWriteBit;
            srcStage = PipelineStageFlags.TopOfPipeBit;
            dstStage = PipelineStageFlags.EarlyFragmentTestsBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.General)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else throw new Exception("Unsupported Layout Transition");

        cmd.ImageMemoryBarrier(barrierInfo, srcStage, dstStage);
        EndSingleTimeCommands(cmd);
    }
    public void CopyBufferToImage(Buffer buffer, Image image, ImageLayout layout, Format format, Extent3D imageExtent, uint mipLevel = 0, uint layerCount = 1)
    {
        var cmd = BeginSingleTimeCommands();
        var range = new ImageSubresourceLayers(null, mipLevel, 0, layerCount);
        if (layout == ImageLayout.DepthStencilAttachmentOptimal)
        {
            range.AspectMask = ImageAspectFlags.DepthBit;
            if (format is Format.D32SfloatS8Uint or Format.D24UnormS8Uint)
                range.AspectMask |= ImageAspectFlags.StencilBit;
        }
        else range.AspectMask = ImageAspectFlags.ColorBit;

        var copyRegion = new BufferImageCopy(0, 0, 0, range, default, imageExtent);
        cmd.CopyBufferToImage(buffer, image, layout, copyRegion);
        EndSingleTimeCommands(cmd);
    }
    
    public CommandBuffer BeginSingleTimeCommands()
    {
        var cmd = AllocateCommandBuffers(1)[0];
        cmd.Begin();
        return cmd;
    }

    public unsafe void EndSingleTimeCommands(CommandBuffer cmd)
    {
        cmd.End();
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd.VkCommandBuffer
        };
        _device.SubmitMainQueue(submitInfo, default);
        _device.WaitForQueue();
        _device.FreeCommandBuffer(cmd, _commandPool);
    }
    
    public void Dispose()
    {
        _allocator.Dispose();
        _commandPool.Dispose();
        _device.Dispose();
        _surface.Dispose();
        _instance.Dispose();
    }
}