using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace TennisApp.Services
{
    public class WebSocketWrapper : IWebSocketWrapper
    {
        private readonly ClientWebSocket _webSocket;

        public WebSocketWrapper()
        {
            _webSocket = new ClientWebSocket();
        }

        public WebSocketState State => _webSocket.State;

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            return _webSocket.ConnectAsync(uri, cancellationToken);
        }

        public Task SendAsync(
            ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken
        )
        {
            return _webSocket.SendAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public Task<WebSocketReceiveResult> ReceiveAsync(
            ArraySegment<byte> buffer,
            CancellationToken cancellationToken
        )
        {
            return _webSocket.ReceiveAsync(buffer, cancellationToken);
        }

        public Task CloseAsync(
            WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken
        )
        {
            return _webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }

        public void Dispose()
        {
            _webSocket.Dispose();
        }
    }
}
