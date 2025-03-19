using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Moq;
using TennisApp.Models;
using TennisApp.Services;
using TennisApp.ViewModels;
using Xunit;

namespace TennisApp.Tests
{
    public class MainPageViewModelTests
    {
        private readonly Mock<ICourtAvailabilityService> _mockCourtAvailabilityService;
        private readonly TestMainThreadService _mainThreadService;
        private readonly MainPageViewModel _viewModel;

        public MainPageViewModelTests()
        {
            // Use MockBehavior.Loose to avoid NullReferenceException for events
            _mockCourtAvailabilityService = new Mock<ICourtAvailabilityService>(MockBehavior.Loose);
            _mainThreadService = new TestMainThreadService();

            // Mock GetCurrentCourts to return an empty list by default
            _mockCourtAvailabilityService
                .Setup(service => service.GetCurrentCourts())
                .Returns(new List<CourtItem>());

            _viewModel = new MainPageViewModel(
                _mockCourtAvailabilityService.Object,
                _mainThreadService
            );
        }

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Arrange & Act (done in the constructor)

            // Assert
            Assert.NotNull(_viewModel.AvailableCourts);
            Assert.Empty(_viewModel.AvailableCourts);
            Assert.False(_viewModel.IsLoading);
            Assert.Empty(_viewModel.ErrorMessage);
            Assert.False(_viewModel.IsConnected);
            Assert.Equal("No courts loaded", _viewModel.DebugText);
        }

        [Fact]
        public async Task OnViewAppearing_ShouldStartListening()
        {
            // Arrange
            _mockCourtAvailabilityService
                .Setup(service => service.StartListeningForCourtUpdatesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.OnViewAppearing();

            // Assert
            _mockCourtAvailabilityService.Verify(
                service => service.StartListeningForCourtUpdatesAsync(),
                Times.Once
            );
        }

        [Fact]
        public async Task StartListeningAsync_ShouldSetIsLoadingAndIsConnected()
        {
            // Arrange
            _mockCourtAvailabilityService
                .Setup(service => service.StartListeningForCourtUpdatesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _viewModel.StartListeningAsync();

            // Assert
            Assert.False(_viewModel.IsLoading);
            Assert.True(_viewModel.IsConnected);
            Assert.Empty(_viewModel.ErrorMessage);
        }

        [Fact]
        public async Task StartListeningAsync_ShouldHandleErrors()
        {
            // Arrange
            var exceptionMessage = "Test exception";
            _mockCourtAvailabilityService
                .Setup(service => service.StartListeningForCourtUpdatesAsync())
                .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            await _viewModel.StartListeningAsync();

            // Assert
            Assert.False(_viewModel.IsLoading);
            Assert.False(_viewModel.IsConnected);
            Assert.Equal(
                $"Failed to connect to server: {exceptionMessage}",
                _viewModel.ErrorMessage
            );
        }

        [Fact]
        public async Task OnCourtAvailabilityChanged_ShouldUpdateCourtsList()
        {
            // Arrange
            await _viewModel.OnViewAppearing(); // Ensure view is active
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

            // Act
            _viewModel.OnCourtAvailabilityChanged(null, courts);
            await Task.Delay(50); // Allow async UI updates to complete

            // Assert
            Assert.Equal(2, _viewModel.AvailableCourts.Count);
            Assert.Equal("Court 1", _viewModel.AvailableCourts[0].Name);
            Assert.False(_viewModel.AvailableCourts[1].IsAvailable);
        }

        [Fact]
        public void OnCourtAvailabilityChanged_ShouldNotUpdateCourtsList_WhenViewIsNotActive()
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
            };

            // Simulate view not being active
            _viewModel.OnViewDisappearing().Wait();

            // Act
            _viewModel.OnCourtAvailabilityChanged(null, courts);

            // Assert
            Assert.Empty(_viewModel.AvailableCourts);
        }

        [Fact]
        public async Task CleanupAsync_ShouldUnsubscribeFromEvents()
        {
            // Arrange
            await _viewModel.OnViewAppearing(); // Ensure view is active
            var courts = new List<CourtItem>
            {
                new CourtItem
                {
                    Id = 1,
                    Name = "Court 1",
                    IsAvailable = true,
                },
            };

            // Act
            await _viewModel.CleanupAsync();
            // Try to trigger the event after cleanup
            _mockCourtAvailabilityService.Raise(
                s => s.CourtAvailabilityChanged += null,
                null,
                courts
            );

            // Assert
            Assert.Empty(_viewModel.AvailableCourts);
        }
    }
}
