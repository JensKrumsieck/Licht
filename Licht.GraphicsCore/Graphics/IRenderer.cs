namespace Licht.GraphicsCore.Graphics;

public interface IRenderer
{
    public ICommandList BeginFrame();
    public void EndFrame();

    public void BeginRenderPass(ICommandList cmd);
    public void EndRenderPass(ICommandList cmd);
}
