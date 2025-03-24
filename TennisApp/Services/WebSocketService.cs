using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TennisApp.Services
{
    public class WebSocketService : IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
        private readonly object _stateLock = new object(); // Lock for thread safety

        public WebSocketService()
        {
            _webSocket = new ClientWebSocket();
            _cancellationTokenSource = new CancellationTokenSource();

            // Register this instance with the App for lifecycle management
            App.RegisterWebSocket(this);
        }

        public async Task ConnectAsync(string url)
        {
            if (_webSocket == null)
            {
                throw new ObjectDisposedException(nameof(WebSocketService));
            }

            try
            {
                // Check if already connected
                if (IsConnected)
                {
                    Console.WriteLine("WebSocket is already connected");

                    throw new InvalidOperationException("WebSocket is already connected");
                    // return;
                }

                // Reset WebSocket if it was previously used
                if (_webSocket.State != WebSocketState.None)
                {
                    lock (_stateLock)
                    {
                        _webSocket.Dispose();
                        _webSocket = new ClientWebSocket();

                        // Also create a new cancellation token source
                        if (_cancellationTokenSource != null)
                        {
                            _cancellationTokenSource.Dispose();
                        }
                        _cancellationTokenSource = new CancellationTokenSource();
                    }
                }

                Console.WriteLine($"Connecting to WebSocket at {url}");

                // Add a connection timeout
                var connectionToken = new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token;
                await _webSocket.ConnectAsync(new Uri(url), connectionToken);

                Console.WriteLine("WebSocket connected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error: {ex.Message}");
                throw;
            }
        }

        public async Task SubscribeToTopicAsync(string topic)
        {
            ThrowIfDisposed();

            if (!IsConnected)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var request = new { action = "subscribe", topic = topic };

            string message = JsonSerializer.Serialize(request);
            Console.WriteLine($"Subscribing to topic: {topic}");
            await SendAsync(message);
        }

        public async Task SendMessageToTopicAsync(string topic, string message)
        {
            ThrowIfDisposed();

            if (!IsConnected)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var request = new
            {
                action = "message",
                topic = topic,
                message = message,
            };

            string serializedMessage = JsonSerializer.Serialize(request);
            Console.WriteLine($"Sending message to topic {topic}: {message}");
            await SendAsync(serializedMessage);
        }

        public async Task SendAsync(string message)
        {
            ThrowIfDisposed();

            if (!IsConnected)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            try
            {
                Console.WriteLine($"Sending WebSocket message: {message}");
                var buffer = Encoding.UTF8.GetBytes(message);

                // Use a local reference to avoid race conditions
                var ws = _webSocket;
                if (ws.State == WebSocketState.Open)
                {
                    await ws.SendAsync(
                        new ArraySegment<byte>(buffer),
                        WebSocketMessageType.Text,
                        true,
                        _cancellationTokenSource.Token
                    );
                }
                else
                {
                    throw new InvalidOperationException(
                        $"WebSocket is in {ws.State} state, not Open"
                    );
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocket send error: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ReceiveAsync()
        {
            ThrowIfDisposed();

            // Use local reference to avoid race conditions
            ClientWebSocket ws;
            lock (_stateLock)
            {
                ws = _webSocket;
            }

            if (ws.State != WebSocketState.Open)
            {
                throw new InvalidOperationException(
                    $"WebSocket is not connected. Current state: {ws.State}"
                );
            }

            try
            {
                var buffer = new byte[4096]; // Larger buffer for potential large messages
                var result = await ws.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cancellationTokenSource.Token
                );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    // Only attempt to close if still in a valid state
                    if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                    {
                        try
                        {
                            await ws.CloseAsync(
                                WebSocketCloseStatus.NormalClosure,
                                "Closed by the client",
                                CancellationToken.None
                            );
                        }
                        catch (WebSocketException ex)
                        {
                            Console.WriteLine(
                                $"Error during WebSocket close after receiving Close message: {ex.Message}"
                            );
                            // Continue anyway - we know it's closed
                        }
                    }
                    return "Connection closed";
                }

                // Extract the actual message content
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);

                // Log received message for debugging (but truncate if very long)
                if (receivedMessage.Length > 200)
                {
                    Console.WriteLine(
                        $"Received WebSocket message ({receivedMessage.Length} chars): {receivedMessage.Substring(0, 200)}..."
                    );
                }
                else
                {
                    Console.WriteLine($"Received WebSocket message: {receivedMessage}");
                }

                // Validate if this looks like JSON
                if (receivedMessage.StartsWith("Connection closed"))
                {
                    Console.WriteLine("Received connection closed message");
                    return "Connection closed";
                }
                else if (!IsValidJson(receivedMessage))
                {
                    Console.WriteLine($"Received non-JSON message: {receivedMessage}");
                    return ""; // Return empty string for non-JSON messages
                }

                return receivedMessage;
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocket receive error: {ex.Message}");
                throw;
            }
        }

        private bool IsValidJson(string message)
        {
            // Quick check if it starts with { or [ (basic JSON structure)
            if (
                string.IsNullOrWhiteSpace(message)
                || (!message.TrimStart().StartsWith("{") && !message.TrimStart().StartsWith("["))
            )
            {
                return false;
            }

            try
            {
                // Try to parse it as JSON using System.Text.Json
                using (JsonDocument.Parse(message))
                {
                    return true;
                }
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public async Task CloseAsync()
        {
            if (_disposed)
                return;

            // Use local reference and check if already closed
            ClientWebSocket ws;
            lock (_stateLock)
            {
                ws = _webSocket;
                if (ws == null || ws.State != WebSocketState.Open)
                {
                    // Already closed or not connected
                    App.UnregisterWebSocket(this);
                    Console.WriteLine(
                        $"WebSocket already in {(ws != null ? ws.State.ToString() : "null")} state, skipping close"
                    );
                    return;
                }
            }

            try
            {
                Console.WriteLine("Attempting to close WebSocket connection...");
                // Use a shorter timeout for closing to prevent UI hangs
                var closeToken = new CancellationTokenSource(5000).Token;
                await ws.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closed by the client",
                    closeToken
                );
                Console.WriteLine("WebSocket closed successfully");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("WebSocket close operation timed out");
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"WebSocket exception during close: {ex.Message}");
                // If there's an exception due to invalid state, we still want to unregister
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error closing WebSocket: {ex.Message}");
            }
            finally
            {
                // Unregister this instance
                App.UnregisterWebSocket(this);
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (_stateLock)
                {
                    return _webSocket != null && _webSocket.State == WebSocketState.Open;
                }
            }
        }

        // IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                lock (_stateLock)
                {
                    // First try to close the connection properly
                    try
                    {
                        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
                        {
                            // Try to close it properly, but don't wait for long
                            _webSocket
                                .CloseOutputAsync(
                                    WebSocketCloseStatus.NormalClosure,
                                    "Disposed by the client",
                                    CancellationToken.None
                                )
                                .Wait(1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during WebSocket disposal: {ex.Message}");
                    }

                    // Cancel any ongoing operations
                    if (_cancellationTokenSource != null)
                    {
                        if (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            _cancellationTokenSource.Cancel();
                        }
                        _cancellationTokenSource.Dispose();
                        _cancellationTokenSource = null;
                    }

                    // Dispose of the WebSocket
                    if (_webSocket != null)
                    {
                        _webSocket.Dispose();
                        _webSocket = null;
                    }
                }

                // Unregister this instance
                App.UnregisterWebSocket(this);
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WebSocketService));
            }
        }

        ~WebSocketService()
        {
            Dispose(false);
        }
    }
}
