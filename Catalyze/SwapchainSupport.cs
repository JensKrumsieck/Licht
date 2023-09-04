using Silk.NET.Vulkan;

namespace Catalyze;

public record struct SwapchainSupport(
    SurfaceCapabilitiesKHR Capabilities,
    SurfaceFormatKHR[] Formats,
    PresentModeKHR[] PresentModes);
