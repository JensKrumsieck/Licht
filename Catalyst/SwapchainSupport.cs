using Silk.NET.Vulkan;

namespace Catalyst;

public record struct SwapchainSupport(
    SurfaceCapabilitiesKHR Capabilities,
    SurfaceFormatKHR[] Formats,
    PresentModeKHR[] PresentModes);
