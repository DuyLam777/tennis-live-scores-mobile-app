namespace TennisApp.Tests.TestHelpers
{
    public class TestMainThreadService : IMainThreadService
    {
        public bool IsMainThread => true;

        public Task InvokeOnMainThreadAsync(Action action)
        {
            action.Invoke();
            return Task.CompletedTask;
        }
    }

    public interface IMainThreadService
    {
        bool IsMainThread { get; }
        Task InvokeOnMainThreadAsync(Action action);
    }
}
