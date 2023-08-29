using System.Numerics;
using ImGuiNET;

namespace RayTracing;

public class Settings
{
    public bool Accumulate = true;
    public bool UseWorld = true;
    public float Exposure = .5f;
    public Vector3 WorldColor = new(.6f, .7f, .9f);
    public bool Blur = false;
    public BlurSettings BlurSettings = new();
}

public class StyleSettings
{
    public Vector2 FramePadding = new(4, 4);
    public Vector2 CellPadding = new(4, 2);
    public Vector2 ItemSpacing = new(6, 4);
    public Vector2 ItemInnerSpacing = new(6, 4);
    public float ScrollbarSize = 16;
    public float WindowRounding = 8;
    public float FrameRounding = 4;
    public float GrabRounding = 4;
    public float TabRounding = 2;

    public void Set()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, FramePadding);
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, CellPadding);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ItemSpacing);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, ItemInnerSpacing);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, ScrollbarSize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, WindowRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, FrameRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, GrabRounding);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, TabRounding);
    }
}

public struct BlurSettings
{
    public int KernelSize = 3;
    public float Sigma = 1f;
    public BlurSettings() {}
}