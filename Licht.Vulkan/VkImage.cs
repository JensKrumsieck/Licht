using Licht.Vulkan.Extensions;
using Licht.Vulkan.Memory;
using Silk.NET.Vulkan;
using SkiaSharp;

namespace Licht.Vulkan;

public unsafe class VkImage : IDisposable
{
    private readonly VkGraphicsDevice _device;

    private AllocatedImage _allocatedImage;
    private ImageView _imageView;
    private Sampler _sampler;
    public Extent3D ImageExtent => new(Width, Height, 1);

    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public readonly Format ImageFormat;
    private readonly ImageLayout _desiredLayout;
    private ImageLayout _currentLayout;
    private readonly ImageUsageFlags _imageUsage;

    public Image Image => _allocatedImage.Image;
    public Allocation Allocation => _allocatedImage.Allocation;

    public VkImage(VkGraphicsDevice device, uint width, uint height, Format imageFormat, ImageLayout desiredLayout,
        ImageUsageFlags flags = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit, void* data = null)
    {
        _device = device;
        Width = width;
        Height = height;
        ImageFormat = imageFormat;
        _desiredLayout = desiredLayout;
        _currentLayout = ImageLayout.Undefined;
        _imageUsage = flags;
        AllocateImage();
        if (data != null)
            SetData(data);
    }

    public VkImage(VkGraphicsDevice device, string filename, ImageLayout desiredLayout,
        Format imageFormat = Format.R8G8B8A8Unorm,
        ImageUsageFlags flags = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit)
    {
        _device = device;
        var image = IOHelper.LoadImageFromFile(filename, imageFormat.ToSkFormat());
        var pixels = image.GetPixels();
        Width = (uint) image.Width;
        Height = (uint) image.Height;
        ImageFormat = imageFormat;
        _desiredLayout = desiredLayout;
        _currentLayout = ImageLayout.Undefined;
        _imageUsage = flags;
        AllocateImage();
        SetData((void*) pixels);
    }

    public VkImage(VkGraphicsDevice device, uint width, uint height, Format imageFormat,
        ImageUsageFlags flags = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferDstBit, void* data = null)
    {
        _device = device;
        Width = width;
        Height = height;
        ImageFormat = imageFormat;
        _imageUsage = flags;
        _desiredLayout = ImageLayout.ShaderReadOnlyOptimal;
        _currentLayout = ImageLayout.Undefined;
        AllocateImage();
        if (data != null)
            SetData(data);
    }

    private void AllocateImage()
    {
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = ImageFormat,
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = _imageUsage,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
            Extent = ImageExtent
        };
        _allocatedImage = _device.CreateImage(imageInfo, MemoryPropertyFlags.DeviceLocalBit);

        var imageViewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _allocatedImage.Image,
            ViewType = ImageViewType.Type2D,
            Format = ImageFormat,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };
        _imageView = _device.CreateImageView(imageViewInfo);

        var samplerCreateInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MinLod = -1000,
            MaxLod = 1000,
            MaxAnisotropy = 1.0f
        };
        _sampler = _device.CreateSampler(samplerCreateInfo);
    }

    public void SetData(void* data)
    {
        var size = FormatHelper.SizeOf(ImageFormat) * Width * Height;
        using var stagingBuffer = VkBuffer.CreateAndCopyToStagingBuffer(_device, data, size);
        TransitionLayoutImmediate(ImageLayout.TransferDstOptimal, 1, 1);
        stagingBuffer.CopyToImage(this);
        TransitionLayoutImmediate(_desiredLayout, 1, 1);
    }

    public DescriptorImageInfo ImageInfo => new()
    {
        Sampler = _sampler,
        ImageView = _imageView,
        ImageLayout = _desiredLayout
    };

    public void TransitionLayoutImmediate(ImageLayout newLayout, uint mipLevels, uint layerCount)
    {
        var cmd = _device.BeginSingleTimeCommands();
        TransitionLayout(cmd, newLayout, mipLevels, layerCount);
        _device.EndSingleTimeCommands(cmd);
    }

    public void TransitionLayout(VkCommandBuffer cmd, ImageLayout newLayout, uint mipLevels, uint layerCount)
    {
        var range = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, mipLevels, 0, layerCount);
        var barrierInfo = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = _currentLayout,
            NewLayout = newLayout,
            Image = Image,
            SubresourceRange = range,
        };

        //determining AccessMasks and PipelineStageFlags from layouts
        PipelineStageFlags srcStage;
        PipelineStageFlags dstStage;

        if (_currentLayout == ImageLayout.Undefined)
        {
            barrierInfo.SrcAccessMask = 0;
            srcStage = PipelineStageFlags.TopOfPipeBit;
        }
        else if (_currentLayout == ImageLayout.General)
        {
            barrierInfo.SrcAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (_currentLayout == ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferReadBit;
            srcStage = PipelineStageFlags.TransferBit;
        }
        else if (_currentLayout == ImageLayout.TransferDstOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TransferBit;
        }
        else if (_currentLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.FragmentShaderBit;
        }
        else throw new Exception($"Currently unsupported Layout Transition from {_currentLayout} to {newLayout}");

        if (newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.DstAccessMask = AccessFlags.TransferReadBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (newLayout == ImageLayout.TransferDstOptimal)
        {
            barrierInfo.DstAccessMask = AccessFlags.TransferWriteBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (newLayout == ImageLayout.General)
        {
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else
            throw new Exception($"Currently unsupported Layout Transition from {_currentLayout} to {newLayout}");

        cmd.ImageMemoryBarrier(barrierInfo, srcStage, dstStage);
        _currentLayout = newLayout;
    }

    public void CopyToBufferImmediate(VkBuffer buffer, uint mipLevels, uint layerCount)
    {
        //check if usable as transfer source -> transition if not
        var tmpLayout = _currentLayout;
        if (_currentLayout != ImageLayout.TransferSrcOptimal)
            TransitionLayoutImmediate(ImageLayout.TransferSrcOptimal, mipLevels, layerCount);

        var cmd = _device.BeginSingleTimeCommands();
        CopyToBuffer(cmd, buffer, layerCount);
        _device.EndSingleTimeCommands(cmd);

        //transfer back to original layout if changed
        if (_currentLayout != tmpLayout)
            TransitionLayoutImmediate(ImageLayout.General, mipLevels, layerCount);
    }

    public void CopyToBuffer(VkCommandBuffer cmd, VkBuffer buffer, uint layerCount)
    {
        var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, layerCount);
        var copyRegion = new BufferImageCopy(0, 0, 0, layers, default, ImageExtent);
        cmd.CopyImageToBuffer(this, buffer, _currentLayout, copyRegion);
    }

    public void Save(string destination)
    {
        var imageData = new uint[Width * Height];
        fixed (void* pImageData = imageData)
        {
            CopyTo(pImageData);

            //color type is hardcoded! convert if needed
            var info = new SKImageInfo((int) Width, (int) Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bmp = new SKBitmap();

            bmp.InstallPixels(info, (nint) pImageData, info.RowBytes);
            using var fs = File.Create(destination);
            bmp.Encode(fs, SKEncodedImageFormat.Png, 100);
        }
    }

    public void CopyTo(void* destination)
    {
        //this is valid for R8G8B8A8 formats and permutations only
        var size = Width * Height * 4;
        using var buffer = new VkBuffer(_device, size, BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit |
            MemoryPropertyFlags.HostCachedBit);
        CopyToBufferImmediate(buffer, 1, 1);

        //copy data using a staging buffer
        void* mappedData = default;
        buffer.AllocatedBuffer.Allocation.Map(ref mappedData).Validate();
        System.Buffer.MemoryCopy(mappedData, destination, size, size);
        buffer.AllocatedBuffer.Allocation.Unmap();
    }
    public void Resize(uint width, uint height)
    {
        if (Height == height && Width == width) return;
        Width = width;
        Height = height;
        Dispose();
        AllocateImage();
    }

    public static implicit operator Image(VkImage i) => i._allocatedImage.Image;

    public void Dispose()
    {
        _device.WaitIdle();
        _device.DestroyImage(_allocatedImage);
        _device.DestroyImageView(_imageView);
        _device.DestroySampler(_sampler);
        GC.SuppressFinalize(this);
    }
}
