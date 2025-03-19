namespace TennisApp.Services
{
    public interface IMainThreadService
    {
        bool IsMainThread { get; }
        Task InvokeOnMainThreadAsync(Action action);
    }

    public class TestMainThreadService : IMainThreadService
    {
        public bool IsMainThread => true;

        public Task InvokeOnMainThreadAsync(Action action) => Task.Run(action);
    }
}
