namespace TennisApp.Services
{
    public interface IWebSocketService
    {
        bool IsConnected { get; }
        Task ConnectAsync(string url);
        Task SendAsync(string message);
        Task<string> ReceiveAsync();
        Task CloseAsync();
    }
}