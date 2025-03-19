using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using TennisApp.Models;
using TennisApp.Services;
using Xunit;

namespace TennisApp.Tests
{
    public class CourtAvailabilityServiceTests
    {
        private readonly Mock<WebSocketService> _mockWebSocketService;
        private readonly CourtAvailabilityService _courtAvailabilityService;
        private readonly string _websocketUrl = "ws://example.com";

        public CourtAvailabilityServiceTests()
        {
            _mockWebSocketService = new Mock<WebSocketService>(
                MockBehavior.Strict,
                new Mock<IWebSocketWrapper>().Object
            );
            _courtAvailabilityService = new CourtAvailabilityService(
                _mockWebSocketService.Object,
                _websocketUrl
            );
        }

        [Fact]
        public async Task StartListeningForCourtUpdatesAsync_ShouldConnectAndSubscribe()
        {
            // Arrange
            _mockWebSocketService.Setup(ws => ws.IsConnected).Returns(false);
            _mockWebSocketService
                .Setup(ws => ws.ConnectAsync(_websocketUrl))
                .Returns(Task.CompletedTask);
            _mockWebSocketService
                .Setup(ws => ws.SendAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();

            // Assert
            _mockWebSocketService.Verify(ws => ws.ConnectAsync(_websocketUrl), Times.Once);
            _mockWebSocketService.Verify(
                ws => ws.SendAsync(It.Is<string>(s => s.Contains("subscribe"))),
                Times.Once
            );
        }

        [Fact]
        public async Task StartListeningForCourtUpdatesAsync_ShouldNotConnectIfAlreadyConnected()
        {
            // Arrange
            _mockWebSocketService.Setup(ws => ws.IsConnected).Returns(true);
            _mockWebSocketService
                .Setup(ws => ws.SendAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();

            // Assert
            _mockWebSocketService.Verify(ws => ws.ConnectAsync(_websocketUrl), Times.Never);
            _mockWebSocketService.Verify(
                ws => ws.SendAsync(It.Is<string>(s => s.Contains("subscribe"))),
                Times.Once
            );
        }

        [Fact]
        public async Task StopListeningForCourtUpdatesAsync_ShouldUnsubscribeAndCancel()
        {
            // Arrange
            _mockWebSocketService.Setup(ws => ws.IsConnected).Returns(true);
            _mockWebSocketService
                .Setup(ws => ws.SendAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Start listening first
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();

            // Act
            await _courtAvailabilityService.StopListeningForCourtUpdatesAsync();

            // Assert
            _mockWebSocketService.Verify(
                ws => ws.SendAsync(It.Is<string>(s => s.Contains("unsubscribe"))),
                Times.Once
            );
        }

        [Fact]
        public async Task ListenForUpdatesAsync_ShouldProcessCourtUpdates()
        {
            // Arrange
            var courts = new List<CourtItem>
            {
                new CourtItem
                {
                    Id = 1,
                    Name = "Court 1",
                    IsAvailable = true,
                },
                new CourtItem
                {
                    Id = 2,
                    Name = "Court 2",
                    IsAvailable = false,
                },
            };

            var message = JsonSerializer.Serialize(
                new WebSocketMessage { Type = "court_availability", Data = courts }
            );

            var subscribeMessage = JsonSerializer.Serialize(
                new WebSocketRequest { Action = "subscribe", Topic = "court_availability" }
            );

            _mockWebSocketService.Setup(ws => ws.IsConnected).Returns(true);
            _mockWebSocketService.Setup(ws => ws.ReceiveAsync()).ReturnsAsync(message);
            _mockWebSocketService
                .Setup(ws =>
                    ws.SendAsync(
                        It.Is<string>(s =>
                            s.Contains("subscribe") && s.Contains("court_availability")
                        )
                    )
                )
                .Returns(Task.CompletedTask);

            // Act
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();
            await Task.Delay(100); // Allow background task to process

            // Assert
            var currentCourts = _courtAvailabilityService.GetCurrentCourts();
            Assert.Equal(2, currentCourts.Count);
            Assert.Equal("Court 1", currentCourts[0].Name);
            Assert.False(currentCourts[1].IsAvailable);

            // Verify the subscription was sent
            _mockWebSocketService.Verify(
                ws =>
                    ws.SendAsync(
                        It.Is<string>(s =>
                            s.Contains("subscribe") && s.Contains("court_availability")
                        )
                    ),
                Times.Once
            );
        }

        [Fact]
        public void ProcessCourtUpdateMessage_ShouldHandleInvalidJson()
        {
            // Arrange
            var invalidMessage = "invalid json :(";

            // Act & Assert
            var ex = Record.Exception(
                () => _courtAvailabilityService.ProcessCourtUpdateMessage(invalidMessage)
            );
            Assert.Null(ex); // Ensure no exception is thrown
        }
    }
}
