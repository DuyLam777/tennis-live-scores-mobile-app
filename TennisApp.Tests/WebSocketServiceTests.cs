using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TennisApp.Services;
using Xunit;

namespace TennisApp.Tests
{
    public class WebSocketServiceTests
    {
        private readonly Mock<IWebSocketWrapper> _mockWebSocket;
        private readonly WebSocketService _webSocketService;

        public WebSocketServiceTests()
        {
            _mockWebSocket = new Mock<IWebSocketWrapper>();
            _webSocketService = new WebSocketService(_mockWebSocket.Object);
        }

        [Fact]
        public async Task ConnectAsync_ShouldConnectToWebSocket()
        {
            // Arrange
            var url = "ws://example.com";
            _mockWebSocket
                .Setup(ws => ws.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _webSocketService.ConnectAsync(url);

            // Assert
            _mockWebSocket.Verify(
                ws =>
                    ws.ConnectAsync(
                        It.Is<Uri>(u => u.ToString() == url + "/"),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task ConnectAsync_ShouldThrowException_WhenConnectionFails()
        {
            // Arrange
            var url = "ws://example.com";
            _mockWebSocket
                .Setup(ws => ws.ConnectAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new WebSocketException("Connection failed"));

            // Act & Assert
            await Assert.ThrowsAsync<WebSocketException>(() => _webSocketService.ConnectAsync(url));
        }

        [Fact]
        public async Task SendAsync_ShouldSendMessage_WhenWebSocketIsOpen()
        {
            // Arrange
            var message = "Test message";
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
            _mockWebSocket
                .Setup(ws =>
                    ws.SendAsync(
                        It.IsAny<ArraySegment<byte>>(),
                        WebSocketMessageType.Text,
                        true,
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);

            // Act
            await _webSocketService.SendAsync(message);

            // Assert
            _mockWebSocket.Verify(
                ws =>
                    ws.SendAsync(
                        It.Is<ArraySegment<byte>>(b =>
                            Encoding.UTF8.GetString(b.Array, b.Offset, b.Count) == message
                        ),
                        WebSocketMessageType.Text,
                        true,
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task SendAsync_ShouldThrowException_WhenWebSocketIsNotOpen()
        {
            // Arrange
            var message = "Test message";
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Closed);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _webSocketService.SendAsync(message)
            );
        }

        [Fact]
        public async Task ReceiveAsync_ShouldReceiveMessage_WhenWebSocketIsOpen()
        {
            // Arrange
            var expectedMessage = "\0\0\0\0\0\0\0\0\0\0\0\0";
            var buffer = Encoding.UTF8.GetBytes(expectedMessage);
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
            _mockWebSocket
                .Setup(ws =>
                    ws.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<CancellationToken>())
                )
                .ReturnsAsync(
                    new WebSocketReceiveResult(buffer.Length, WebSocketMessageType.Text, true)
                );

            // Act
            var result = await _webSocketService.ReceiveAsync();

            // Assert
            Assert.Equal(expectedMessage, result);
        }

        [Fact]
        public async Task ReceiveAsync_ShouldThrowException_WhenWebSocketIsNotOpen()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Closed);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _webSocketService.ReceiveAsync()
            );
        }

        [Fact]
        public async Task CloseAsync_ShouldCloseWebSocket_WhenWebSocketIsOpen()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);
            _mockWebSocket
                .Setup(ws =>
                    ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by the client",
                        It.IsAny<CancellationToken>()
                    )
                )
                .Returns(Task.CompletedTask);

            // Act
            await _webSocketService.CloseAsync();

            // Assert
            _mockWebSocket.Verify(
                ws =>
                    ws.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by the client",
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
        }

        [Fact]
        public async Task CloseAsync_ShouldNotThrowException_WhenWebSocketIsAlreadyClosed()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Closed);

            // Act
            await _webSocketService.CloseAsync();

            // Assert
            _mockWebSocket.Verify(
                ws =>
                    ws.CloseAsync(
                        It.IsAny<WebSocketCloseStatus>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Never
            );
        }

        [Fact]
        public void IsConnected_ShouldReturnTrue_WhenWebSocketIsOpen()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Open);

            // Act
            var isConnected = _webSocketService.IsConnected;

            // Assert
            Assert.True(isConnected);
        }

        [Fact]
        public void IsConnected_ShouldReturnFalse_WhenWebSocketIsNotOpen()
        {
            // Arrange
            _mockWebSocket.Setup(ws => ws.State).Returns(WebSocketState.Closed);

            // Act
            var isConnected = _webSocketService.IsConnected;

            // Assert
            Assert.False(isConnected);
        }
    }
}
