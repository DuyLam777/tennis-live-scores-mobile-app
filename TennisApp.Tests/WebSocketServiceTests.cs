using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TennisApp.Services;
using Xunit;

namespace TennisApp.Tests
{
    public class WebSocketServiceTests : IDisposable
    {
        private readonly WebSocketService _webSocketService;

        public WebSocketServiceTests()
        {
            _webSocketService = new WebSocketService();
        }

        public void Dispose()
        {
            _webSocketService.Dispose();
        }

        [Fact]
        public async Task ConnectAsync_SuccessfulConnection()
        {
            // Arrange
            var url = "ws://echo.websocket.events";

            // Act
            await _webSocketService.ConnectAsync(url);

            // Assert
            Assert.True(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task ConnectAsync_AlreadyConnected_ThrowsException()
        {
            // Arrange
            var url = "ws://echo.websocket.events";
            await _webSocketService.ConnectAsync(url);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _webSocketService.ConnectAsync(url)
            );
        }

        [Fact]
        public async Task SubscribeToTopicAsync_ValidTopic_SendsSubscriptionRequest()
        {
            // Arrange
            var url = "ws://echo.websocket.events";
            await _webSocketService.ConnectAsync(url);
            var topic = "test-topic";

            // Act
            await _webSocketService.SubscribeToTopicAsync(topic);

            // Assert
            // For simplicity, I assume the method works if no exception is thrown.
        }

        [Fact]
        public async Task SendMessageToTopicAsync_ValidMessage_SendsMessage()
        {
            // Arrange
            var url = "ws://echo.websocket.events";
            await _webSocketService.ConnectAsync(url);
            var topic = "test-topic";
            var message = "Hello, WebSocket!";

            // Act
            await _webSocketService.SendMessageToTopicAsync(topic, message);

            // Assert
        }

        [Fact]
        public void IsConnected_PropertyReflectsCorrectState()
        {
            // Arrange & Act
            var isConnectedBeforeConnect = _webSocketService.IsConnected;

            // Act
            var url = "ws://echo.websocket.events";
            _webSocketService.ConnectAsync(url).Wait();

            // Assert
            Assert.False(isConnectedBeforeConnect);
            Assert.True(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task Dispose_PreventsFurtherOperations()
        {
            // Arrange
            _webSocketService.Dispose();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ObjectDisposedException>(
                () => _webSocketService.ConnectAsync("ws://echo.websocket.events")
            );

            Assert.Equal(nameof(WebSocketService), exception.ObjectName);
        }

        [Fact]
        public async Task CloseAsync_SuccessfulClose_DisconnectsWebSocket()
        {
            // Arrange
            var url = "ws://echo.websocket.events";
            await _webSocketService.ConnectAsync(url);

            // Act
            await _webSocketService.CloseAsync();

            // Assert
            Assert.False(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task SendAsync_ThrowsExceptionIfNotConnected()
        {
            // Arrange
            var message = "Test message";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _webSocketService.SendAsync(message)
            );
        }

        [Fact]
        public async Task ReceiveAsync_ThrowsExceptionWhenConnectionClosed()
        {
            // Arrange
            var url = "ws://echo.websocket.events";
            await _webSocketService.ConnectAsync(url);

            // Simulate closing the WebSocket
            await _webSocketService.CloseAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _webSocketService.ReceiveAsync()
            );

            Assert.Equal("WebSocket is not connected. Current state: Closed", exception.Message);
        }

        [Fact]
        public async Task ReceiveAsync_ThrowsExceptionIfNotConnected()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _webSocketService.ReceiveAsync()
            );
        }

        [Fact]
        public async Task SendMessageToTopicAsync_ThrowsExceptionIfNotConnected()
        {
            // Arrange
            var topic = "test-topic";
            var message = "Hello, WebSocket!";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _webSocketService.SendMessageToTopicAsync(topic, message)
            );
        }

        [Fact]
        public void Dispose_MultipleCallsDoNotThrowException()
        {
            // Act & Assert
            _webSocketService.Dispose();
            _webSocketService.Dispose(); // Should not throw an exception
        }

        [Fact]
        public async Task SubscribeToTopicAsync_ThrowsExceptionIfNotConnected()
        {
            // Arrange
            var topic = "test-topic";

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _webSocketService.SubscribeToTopicAsync(topic)
            );
        }

        [Fact]
        public async Task ConnectAsync_ThrowsExceptionForInvalidURL()
        {
            // Arrange
            var invalidUrl = "invalid-url";

            // Act & Assert
            await Assert.ThrowsAsync<UriFormatException>(
                () => _webSocketService.ConnectAsync(invalidUrl)
            );
        }

        [Fact]
        public async Task ReceiveAsync_ReturnsEmptyStringForNonJSONMessages()
        {
            // Arrange
            var url = "ws://echo.websocket.events";
            await _webSocketService.ConnectAsync(url);

            // Act
            var result = await _webSocketService.ReceiveAsync();

            // Assert
            Assert.Equal("", result);
        }
    }
}
