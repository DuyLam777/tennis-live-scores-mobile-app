// TennisApp/Services/WebSocketService.cs
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace TennisApp.Services
{
    public class WebSocketService
    {
        private ClientWebSocket? _webSocket;

        public async Task ConnectAsync(string uri)
        {
            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
        }

        public async Task SendAsync(string message)
        {
            if (_webSocket?.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        public async Task<string> ReceiveAsync()
        {
            var buffer = new byte[1024];
            if (_webSocket == null)
            {
                throw new InvalidOperationException("WebSocket is not connected.");
            }
            var result = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
            return Encoding.UTF8.GetString(buffer, 0, result.Count);
        }

        public async Task CloseAsync()
        {
            if (_webSocket != null)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                _webSocket.Dispose();
            }
        }
    }
}