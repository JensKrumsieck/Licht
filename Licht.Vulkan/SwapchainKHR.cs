using Silk.NET.Vulkan;

namespace Licht.Vulkan;
public unsafe partial struct SwapchainKHR
{
    public Result AcquireNextImage(ulong timeout, Semaphore semaphore, Fence fence, ref uint imageIndex)
        => _khrSwapchain.AcquireNextImage(_device, _swapchainKHR, timeout, semaphore, fence, ref imageIndex);

    public Result QueuePresent(Queue queue, PresentInfoKHR presentInfo) => _khrSwapchain.QueuePresent(queue, presentInfo);

    public Result GetSwapchainImages(uint* pCount, Silk.NET.Vulkan.Image* pImages) => _khrSwapchain.GetSwapchainImages(_device, _swapchainKHR, pCount, pImages);
    public Result GetSwapchainImages(uint* pCount, out Image[] images)
    {
        var vkImages = new Silk.NET.Vulkan.Image[*pCount];
        fixed (Silk.NET.Vulkan.Image* pSwapchainImages = vkImages)
        {
            var result = GetSwapchainImages(pCount, pSwapchainImages);
            var d = _device;
            images = Array.ConvertAll(vkImages, i => new Image(d, i));
            return result;
        }
    }
}
