using System.Net.WebSockets;
using System.Text;

namespace TennisApp.Services
{
    public class WebSocketService
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;

        public WebSocketService()
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task ConnectAsync(string url)
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await CloseAsync();
                }

                // Reset WebSocket if it was previously used
                if (_webSocket.State != WebSocketState.None)
                {
                    _webSocket = new ClientWebSocket();
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                await _webSocket.ConnectAsync(new Uri(url), _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error: {ex.Message}");
                throw;
            }
        }

        public async Task SendAsync(string message)
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var buffer = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token
            );
        }

        public async Task<string> ReceiveAsync()
        {
            if (_webSocket.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var buffer = new byte[1024];
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                _cancellationTokenSource.Token
            );

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closed by the client",
                    CancellationToken.None
                );

                return "Connection closed";
            }

            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        public async Task CloseAsync()
        {
            try
            {
                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by the client",
                        CancellationToken.None
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing WebSocket: {ex.Message}");
            }
            finally
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open;
    }
}
