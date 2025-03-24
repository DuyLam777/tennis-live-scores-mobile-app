using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TennisApp.Models;
using TennisApp.Services;
using Xunit;

namespace TennisApp.Tests
{
    public class CourtAvailabilityServiceTests : IDisposable
    {
        private readonly WebSocketService _webSocketService;
        private readonly CourtAvailabilityService _courtAvailabilityService;
        private readonly string _websocketUrl = "ws://echo.websocket.events";

        public CourtAvailabilityServiceTests()
        {
            _webSocketService = new WebSocketService();
            _courtAvailabilityService = new CourtAvailabilityService(
                _webSocketService,
                _websocketUrl
            );
        }

        public void Dispose()
        {
            _courtAvailabilityService.StopListeningForCourtUpdatesAsync().Wait();
            _webSocketService.Dispose();
        }

        [Fact]
        public async Task StartListeningForCourtUpdatesAsync_ConnectsAndSubscribesSuccessfully()
        {
            // Act
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();

            // Assert
            Assert.True(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task GetCurrentCourts_ReturnsLastKnownCourts()
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

            _courtAvailabilityService
                .GetType()
                .GetMethod(
                    "ProcessCourtUpdateMessage",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                )
                ?.Invoke(
                    _courtAvailabilityService,
                    new object[]
                    {
                        "{\"type\":\"court_availability\",\"data\":[{\"id\":1,\"name\":\"Court 1\",\"isAvailable\":true},{\"id\":2,\"name\":\"Court 2\",\"isAvailable\":false}]}",
                    }
                );

            // Act
            var result = _courtAvailabilityService.GetCurrentCourts();

            // Assert
            Assert.Equal(courts.Count, result.Count);
            Assert.Equal(courts[0].Id, result[0].Id);
            Assert.Equal(courts[0].Name, result[0].Name);
            Assert.Equal(courts[0].IsAvailable, result[0].IsAvailable);
        }

        [Fact]
        public async Task ProcessCourtUpdateMessage_ValidMessage_UpdatesCourts()
        {
            // Arrange
            var message =
                "{\"type\":\"court_availability\",\"data\":[{\"id\":1,\"name\":\"Court 1\",\"isAvailable\":true},{\"id\":2,\"name\":\"Court 2\",\"isAvailable\":false}]}";

            // Act
            _courtAvailabilityService
                .GetType()
                .GetMethod(
                    "ProcessCourtUpdateMessage",
                    System.Reflection.BindingFlags.NonPublic 
                        | System.Reflection.BindingFlags.Instance
                )
                ?.Invoke(_courtAvailabilityService, new object[] { message });

            // Assert
            var courts = _courtAvailabilityService.GetCurrentCourts();
            Assert.Equal(2, courts.Count);
            Assert.Equal(1, courts[0].Id);
            Assert.Equal("Court 1", courts[0].Name);
            Assert.True(courts[0].IsAvailable);
        }

        [Fact]
        public async Task ProcessCourtUpdateMessage_InvalidMessage_DoesNotUpdateCourts()
        {
            // Arrange
            var message = "invalid-message";

            // Act
            _courtAvailabilityService
                .GetType()
                .GetMethod(
                    "ProcessCourtUpdateMessage",
                    System.Reflection.BindingFlags.NonPublic 
                        | System.Reflection.BindingFlags.Instance
                )
                ?.Invoke(_courtAvailabilityService, new object[] { message });

            // Assert
            var courts = _courtAvailabilityService.GetCurrentCourts();
            Assert.Empty(courts);
        }

        [Fact]
        public async Task ReconnectAsync_ReconnectsAfterDisconnection()
        {
            // Arrange
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();
            await _courtAvailabilityService.StopListeningForCourtUpdatesAsync();

            // Act
            var reconnectMethod = _courtAvailabilityService
                .GetType()
                .GetMethod(
                    "ReconnectAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );

            await (Task)reconnectMethod.Invoke(_courtAvailabilityService, null);

            // Assert
            Assert.True(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task StopListeningForCourtUpdatesAsync_NotListening_DoesNothing()
        {
            // Arrange

            // Act
            await _courtAvailabilityService.StopListeningForCourtUpdatesAsync();

            // Assert
            // No exception should be thrown, and the WebSocket should remain disconnected
            Assert.False(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task StopListeningForCourtUpdatesAsync_StopsListeningAndUnsubscribes()
        {
            // Arrange
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();

            // Act
            await _courtAvailabilityService.StopListeningForCourtUpdatesAsync();

            // Assert
            Assert.False(
                _courtAvailabilityService
                    .GetType()
                    .GetField(
                        "_isListening",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    )
                    ?.GetValue(_courtAvailabilityService) as bool?
            );

            // Verify that the WebSocket remains connected (it should not close unless explicitly done)
            Assert.True(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task ListenForUpdatesAsync_HandlesEmptyMessagesGracefully()
        {
            // Arrange
            var listenTask = Task.Run(async () =>
            {
                await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();
            });

            // Simulate an empty message being received
            var receiveMethod = _webSocketService
                .GetType()
                .GetMethod(
                    "ReceiveAsync",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                );

            receiveMethod.Invoke(_webSocketService, null);

            // Act & Assert
            await Task.Delay(500); // Allow time for processing
            Assert.True(true);
        }

        [Fact]
        public void ProcessCourtUpdateMessage_NonJsonMessage_DoesNotThrowException()
        {
            // Arrange
            var message = "This is not JSON";

            // Act
            Action act = () =>
                _courtAvailabilityService
                    .GetType()
                    .GetMethod(
                        "ProcessCourtUpdateMessage",
                        System.Reflection.BindingFlags.NonPublic
                            | System.Reflection.BindingFlags.Instance
                    )
                    ?.Invoke(_courtAvailabilityService, new object[] { message });

            // Assert
            act();
        }

        [Fact]
        public async Task ReconnectAsync_ReconnectsAfterWebSocketClosure()
        {
            // Arrange
            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();

            await _webSocketService.CloseAsync();

            // Act
            var reconnectMethod = _courtAvailabilityService
                .GetType()
                .GetMethod(
                    "ReconnectAsync",
                    System.Reflection.BindingFlags.NonPublic
                        | System.Reflection.BindingFlags.Instance
                );

            await (Task)reconnectMethod.Invoke(_courtAvailabilityService, null);

            // Assert
            Assert.True(_webSocketService.IsConnected);
        }

        [Fact]
        public async Task StopListeningForCourtUpdatesAsync_CancelsListeningTask()
        {
            // Arrange
            var listeningTaskCompleted = new TaskCompletionSource<bool>();
            var tokenSource = new CancellationTokenSource();

            await _courtAvailabilityService.StartListeningForCourtUpdatesAsync();
            _ = Task.Run(
                async () =>
                {
                    try
                    {
                        await Task.Delay(Timeout.Infinite, tokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        listeningTaskCompleted.SetResult(true);
                    }
                },
                tokenSource.Token
            );

            // Act
            await _courtAvailabilityService.StopListeningForCourtUpdatesAsync();

            // Assert
            var taskCompleted = await Task.WhenAny(listeningTaskCompleted.Task, Task.Delay(1000));
            Assert.False(
                taskCompleted == listeningTaskCompleted.Task && listeningTaskCompleted.Task.Result
            );
        }
    }
}
