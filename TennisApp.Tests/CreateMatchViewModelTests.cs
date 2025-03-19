using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using TennisApp.Services;
using TennisApp.ViewModels;
using Xunit;

namespace TennisApp.Tests
{
    public class CreateMatchViewModelTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;
        private readonly CreateMatchViewModel _viewModel;
        private readonly TestMainThreadService _mainThreadService;

        public CreateMatchViewModelTests()
        {
            // Setup the mock HTTP message handler
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://test.com/"),
            };
            _mainThreadService = new TestMainThreadService();
            // Initialize the view model with the mocked HTTP client
            _viewModel = new CreateMatchViewModel(_httpClient, _mainThreadService);
        }

        [Fact]
        public async Task LoadData_LoadsDataSuccessfully()
        {
            // Arrange
            var playersJson =
                "[{\"id\":1,\"name\":\"John Doe\"},{\"id\":2,\"name\":\"Jane Smith\"}]";
            var courtsJson = "[{\"id\":1,\"name\":\"Court A\"},{\"id\":2,\"name\":\"Court B\"}]";
            var scoreboardsJson =
                "[{\"id\":1,\"batteryLevel\":85,\"lastConnected\":\"2025-03-19T12:00:00\"},{\"id\":2,\"batteryLevel\":72,\"lastConnected\":\"2025-03-19T11:30:00\"}]";

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri.ToString() == "http://test.com/api/players"
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(playersJson),
                    }
                );

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri.ToString() == "http://test.com/api/courts"
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(courtsJson),
                    }
                );

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri.ToString() == "http://test.com/api/scoreboards"
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(
                    new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(scoreboardsJson),
                    }
                );

            // Act
            await _viewModel.LoadDataCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal(2, _viewModel.AvailablePlayers.Count);
            Assert.Equal(2, _viewModel.AvailableCourts.Count);
            Assert.Equal(2, _viewModel.AvailableScoreboards.Count);
            Assert.False(_viewModel.IsLoading);
        }

        [Fact]
        public async Task LoadData_HandlesNetworkError()
        {
            // Arrange
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri.ToString() == "http://test.com/api/players"
                    ),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            await _viewModel.LoadDataCommand.ExecuteAsync(null);

            // Assert
            Assert.StartsWith("", _viewModel.ErrorMessage);
            Assert.False(_viewModel.IsLoading);
        }

        [Fact]
        public async Task CreateMatchAsync_HandlesInvalidInput()
        {
            // Arrange
            _viewModel.IsLoading = false;
            _viewModel.SelectedPlayer1 = null;
            _viewModel.SelectedPlayer2 = null;
            _viewModel.SelectedCourt = null;
            _viewModel.SelectedScoreboard = null;

            // Act
            await _viewModel.CreateMatchCommand.ExecuteAsync(null);

            // Assert
            Assert.Equal("Please select all required fields", _viewModel.ErrorMessage);
            Assert.False(_viewModel.IsLoading);
        }
    }
}
