namespace Catalyze.Applications;

public interface IAppLayer
{
    void OnAttach(){}
    void OnDetach(){}
    void OnUpdate(double deltaTime){}
    void OnDrawGui(double deltaTime){}
}