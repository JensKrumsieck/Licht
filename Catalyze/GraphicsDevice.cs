using Catalyze.Allocation;
using Catalyze.Applications;
using Catalyze.Tools;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Catalyze;

public unsafe class GraphicsDevice : IAppModule
{
    public readonly Instance VkInstance;
    public readonly DebugUtilsMessengerEXT VkDebugMessenger;
    public readonly PhysicalDevice VkPhysicalDevice;
    public readonly Device VkDevice;
    public readonly SurfaceKHR VkSurface;
    public readonly Queue VkMainDeviceQueue;
    public readonly CommandPool VkCommandPool;
    public readonly uint MainQueueIndex;
    public readonly IAllocator Allocator;
    
    //needed extensions
    private readonly ExtDebugUtils _extDebugUtils;
    private readonly KhrSurface? _khrSurface;

    public GraphicsDevice(IVkSurfaceSource window, IAllocator allocator) : this(new GraphicsDeviceCreateOptions(), window, allocator){}
    
    public GraphicsDevice(GraphicsDeviceCreateOptions options, IVkSurfaceSource window, IAllocator allocator)
    {
        options.Validate();
        
        //create instance
        {
            var appInfo = new ApplicationInfo
            {
                SType = StructureType.ApplicationInfo,
                PApplicationName = new ByteString(options.ApplicationName),
                ApplicationVersion = options.ApplicationVersion,
                PEngineName = new ByteString(options.EngineName),
                EngineVersion = options.EngineVersion,
                ApiVersion = options.ApiVersion
            };
            var extensions = new ByteStringList(options.EnabledInstanceExtensions);
            var layers = new ByteStringList(options.EnabledLayers);
            var createInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &appInfo,
                EnabledExtensionCount = extensions.Count,
                PpEnabledExtensionNames = extensions,
                EnabledLayerCount = layers.Count,
                PpEnabledLayerNames = layers,
                Flags = options.Flags
            };
            if (options.EnableDebug)
            {
                var debugCreateInfo = PrepareDebugCreateInfo();
                createInfo.PNext = &debugCreateInfo;
            }
            vk.CreateInstance(createInfo, null, out VkInstance).Validate();
            if (!vk.TryGetInstanceExtension(VkInstance, out _extDebugUtils))
                throw new Exception($"[Vulkan] Could not find extension {ExtDebugUtils.ExtensionName}");
            _extDebugUtils.CreateDebugUtilsMessenger(VkInstance, PrepareDebugCreateInfo(), null, out VkDebugMessenger).Validate();
        }
            
        //Pick GPU
        {
            var physicalDevices = vk.GetPhysicalDevices(VkInstance).ToArray();
            VkPhysicalDevice = SelectorTools.SelectByScore(physicalDevices, options.PhysicalDeviceSelector);
        }
        
        //Create Device
        {
            var defaultPriority = 1.0f;
            var queueFamilies = VkPhysicalDevice.Enumerate<QueueFamilyProperties>(vk.GetPhysicalDeviceQueueFamilyProperties);
            for (var i = 0u; i < queueFamilies.Length; i++)
            {
                if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit) ||
                    queueFamilies[i].QueueFlags.HasFlag(QueueFlags.ComputeBit))
                {
                    MainQueueIndex = i;
                    break;
                }
            }
            
            var queueCreateInfo = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = MainQueueIndex,
                PQueuePriorities = &defaultPriority
            };
            using var extensions = new ByteStringList(options.EnabledDeviceExtensions);
            using var layers = new ByteStringList(options.EnabledLayers);
            var createInfo = new DeviceCreateInfo
            {
                SType = StructureType.DeviceCreateInfo,
                QueueCreateInfoCount = 1,
                PQueueCreateInfos = &queueCreateInfo,
                PEnabledFeatures = &options.DesiredDeviceFeatures,
                EnabledExtensionCount = extensions.Count,
                PpEnabledExtensionNames = extensions,
                EnabledLayerCount = layers.Count,
                PpEnabledLayerNames = layers
            };
            vk.CreateDevice(VkPhysicalDevice, createInfo, null, out VkDevice).Validate();
            vk.GetDeviceQueue(VkDevice, MainQueueIndex, 0, out VkMainDeviceQueue);
        }
        
        //create surface
        if(options.EnabledInstanceExtensions.Contains(KhrSurface.ExtensionName))
        {
            if (!vk.TryGetInstanceExtension(VkInstance, out _khrSurface))
                throw new Exception($"Could not get instance extension {KhrSurface.ExtensionName}");
            VkSurface = window.VkSurface!
                .Create<AllocationCallbacks>(new VkHandle(VkInstance.Handle), null)
                .ToSurface();
        }

        //create command pool
        {
            var poolInfo = new CommandPoolCreateInfo
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex =  MainQueueIndex,
                Flags = options.CommandPoolCreateFlags
            };
            vk.CreateCommandPool(VkDevice, poolInfo, null, out VkCommandPool).Validate();
        }
        //init allocator
        {
            Allocator = allocator;
            Allocator.Bind(this);
        }
    }
    
    public SwapchainSupport GetSwapchainSupport(PhysicalDevice physicalDevice)
    {
        if (_khrSurface is null) return default;
        _khrSurface!.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, VkSurface, out var capabilities);
        var count = 0u;
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, VkSurface, &count, null);
        var formats = new SurfaceFormatKHR[count];
        _khrSurface.GetPhysicalDeviceSurfaceFormats(physicalDevice, VkSurface, &count, formats);

        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, VkSurface, &count, null);
        var presentModes = new PresentModeKHR[count];
        _khrSurface.GetPhysicalDeviceSurfacePresentModes(physicalDevice, VkSurface, &count, presentModes);
        return new SwapchainSupport(capabilities, formats, presentModes);
    }
    
    public void WaitIdle() => vk.DeviceWaitIdle(VkDevice);
    public Result WaitForFence(Fence fence) => vk.WaitForFences(VkDevice, 1, fence, true, ulong.MaxValue);
    public Result WaitForQueue() => vk.QueueWaitIdle(VkMainDeviceQueue);
    public Result ResetFence(Fence fence) => vk.ResetFences(VkDevice, 1, fence);
    public Result SubmitMainQueue(SubmitInfo submitInfo, Fence fence) => vk.QueueSubmit(VkMainDeviceQueue, 1, submitInfo, fence);
    public CommandBuffer[] AllocateCommandBuffers(uint count)
    {
        var commandBuffers = new CommandBuffer[count];
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            Level = CommandBufferLevel.Primary,
            CommandPool = VkCommandPool,
            CommandBufferCount = count
        };
        fixed (void* pCommandBuffers = commandBuffers)
            vk.AllocateCommandBuffers(VkDevice, allocInfo, (Silk.NET.Vulkan.CommandBuffer*)pCommandBuffers).Validate();
        return commandBuffers;
    }

    public void FreeCommandBuffers(CommandBuffer[] commandBuffers) =>
        vk.FreeCommandBuffers(VkDevice, VkCommandPool, (uint) commandBuffers.Length,
                              commandBuffers.AsArray<CommandBuffer, Silk.NET.Vulkan.CommandBuffer>());
    public void FreeCommandBuffer(CommandBuffer commandBuffer) =>
        vk.FreeCommandBuffers(VkDevice, VkCommandPool, 1, commandBuffer);

    public Format FindFormat(Format[] candidates, ImageTiling tiling, FormatFeatureFlags formatFeatureFlags)
    {
        foreach (var candidate in candidates)
        {
            vk.GetPhysicalDeviceFormatProperties(VkPhysicalDevice, candidate, out var props);
            if (tiling == ImageTiling.Linear && (props.LinearTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
                return candidate;
            if (tiling == ImageTiling.Optimal && (props.OptimalTilingFeatures & formatFeatureFlags) == formatFeatureFlags)
                return candidate;
        }
        throw new Exception($"Unable to find supported format for {tiling} and {formatFeatureFlags}");
    }

    public AllocatedImage CreateImage(ImageCreateInfo info, MemoryPropertyFlags propertyFlags)
    {
        vk.CreateImage(VkDevice, info, null, out var image).Validate();
        var allocInfo = new AllocationCreateInfo {Usage = propertyFlags};
        Allocator.AllocateImage(image, allocInfo, out var allocation);
        return new AllocatedImage(image, allocation);
    }
    public void DestroyImage(AllocatedImage image)
    {
        vk.DestroyImage(VkDevice, image.Image, null);
        image.Allocation.Dispose();
    }
    public ImageView CreateImageView(ImageViewCreateInfo info)
    {
        vk.CreateImageView(VkDevice, info, null, out var imageView).Validate();
        return imageView;
    }
    public void DestroyImageView(ImageView view) => vk.DestroyImageView(VkDevice, view, null);
    public Sampler CreateSampler(SamplerCreateInfo createInfo)
    {
        vk.CreateSampler(VkDevice, createInfo, default, out var sampler).Validate();
        return sampler;
    }
    public void DestroySampler(Sampler sampler) => vk.DestroySampler(VkDevice, sampler, null);
    public Buffer CreateBuffer(uint instanceSize, uint instanceCount, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags)
    {
        ulong alignedSize = instanceSize * instanceCount;
        vk.GetPhysicalDeviceProperties(VkPhysicalDevice, out var props);
        if (usageFlags == BufferUsageFlags.UniformBufferBit) alignedSize = Buffer.GetAlignment(alignedSize, props.Limits.MinUniformBufferOffsetAlignment);
        else alignedSize = Buffer.GetAlignment(alignedSize, 256);
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = alignedSize,
            Usage = usageFlags,
            SharingMode = SharingMode.Exclusive
        };
        vk.CreateBuffer(VkDevice, bufferInfo, null, out var buffer).Validate();
        var allocInfo = new AllocationCreateInfo {Usage = memoryFlags};
        Allocator.AllocateBuffer(buffer, allocInfo, out var allocation);
        return new Buffer(VkDevice, alignedSize, buffer, allocation);
    }
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
    public void EndSingleTimeCommands(CommandBuffer cmd)
    {
        cmd.End();
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd.VkCommandBuffer
        };
        SubmitMainQueue(submitInfo, default);
        WaitForQueue();
        FreeCommandBuffer(cmd);
    }
    private static DebugUtilsMessengerCreateInfoEXT PrepareDebugCreateInfo() => new()
    {
        SType = StructureType.DebugUtilsMessengerCreateInfoExt,
        MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt,
        MessageType = DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt | DebugUtilsMessageTypeFlagsEXT.DeviceAddressBindingBitExt,
        PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugTools.DefaultDebugCallback
    };
    public void Dispose()
    {
        vk.DestroyCommandPool(VkDevice, VkCommandPool, null);
        vk.DestroyDevice(VkDevice, null);
        _khrSurface?.DestroySurface(VkInstance, VkSurface, null);
        _extDebugUtils.DestroyDebugUtilsMessenger(VkInstance, VkDebugMessenger, null);
        vk.DestroyInstance(VkInstance, null);
        
        _extDebugUtils.Dispose();
        _khrSurface?.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
