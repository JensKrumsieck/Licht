namespace Catalyst.Engine;

public interface ILayer
{
    void OnAttach(){}
    void OnDetach(){}
    void OnUpdate(double deltaTime){}
    void OnDrawGui(double deltaTime){}
}