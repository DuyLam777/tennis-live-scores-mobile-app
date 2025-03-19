namespace 
public interface IMainThreadService
{
    bool IsMainThread { get; }
    Task InvokeOnMainThreadAsync(Action action);
}
