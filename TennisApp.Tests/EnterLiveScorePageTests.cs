using System;
using System.Threading.Tasks;
using Moq;
using TennisApp.Config;
using TennisApp.Services;
using TennisApp.Tests.TestHelpers;
using TennisApp.Views;
using Xunit;

namespace TennisApp.Tests.Views
{
    public class EnterLiveScorePageTests
    {
        private readonly Mock<WebSocketService> _mockWebSocketService;

        public EnterLiveScorePageTests()
        {
            ApplicationResourcesSetup.Initialize();
            _mockWebSocketService = new Mock<WebSocketService>();
        }

        [Fact]
        public void MatchId_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var page = new EnterLiveScorePage();
            int expectedId = 123;

            // Act
            page.MatchId = expectedId;

            // Assert
            Assert.Equal(expectedId, page.MatchId);
        }

        [Fact]
        public void MatchTitle_SetValue_ShouldUpdateProperty()
        {
            // Arrange
            var page = new EnterLiveScorePage();
            string expectedTitle = "Test Match";

            // Act
            page.MatchTitle = expectedTitle;

            // Assert
            Assert.Equal(expectedTitle, page.MatchTitle);
        }

        [Fact]
        public void AddGameP1_Clicked_ShouldIncrementPlayer1Games()
        {
            // Arrange
            var page = new EnterLiveScorePage();

            // Use reflection to access private fields
            var player1GamesField = typeof(EnterLiveScorePage).GetField(
                "player1Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            int initialValue = (int)player1GamesField.GetValue(page);

            // Act
            // Simulate button click by directly calling the event handler
            var methodInfo = typeof(EnterLiveScorePage).GetMethod(
                "AddGameP1_Clicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            methodInfo.Invoke(page, new object[] { null, EventArgs.Empty });

            // Assert
            int newValue = (int)player1GamesField.GetValue(page);
            Assert.Equal(initialValue + 1, newValue);
        }

        [Fact]
        public void AddGameP2_Clicked_ShouldIncrementPlayer2Games()
        {
            // Arrange
            var page = new EnterLiveScorePage();

            var player2GamesField = typeof(EnterLiveScorePage).GetField(
                "player2Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            int initialValue = (int)player2GamesField.GetValue(page);

            // Act
            var methodInfo = typeof(EnterLiveScorePage).GetMethod(
                "AddGameP2_Clicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            methodInfo.Invoke(page, new object[] { null, EventArgs.Empty });

            // Assert
            int newValue = (int)player2GamesField.GetValue(page);
            Assert.Equal(initialValue + 1, newValue);
        }

        [Fact]
        public void AddSetP1_Clicked_ShouldIncrementPlayer1SetsAndResetGames()
        {
            // Arrange
            var page = new EnterLiveScorePage();

            var player1SetsField = typeof(EnterLiveScorePage).GetField(
                "player1Sets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var player1GamesField = typeof(EnterLiveScorePage).GetField(
                "player1Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var player2GamesField = typeof(EnterLiveScorePage).GetField(
                "player2Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            int initialSets = (int)player1SetsField.GetValue(page);

            // Act
            var methodInfo = typeof(EnterLiveScorePage).GetMethod(
                "AddSetP1_Clicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            methodInfo.Invoke(page, new object[] { null, EventArgs.Empty });

            // Assert
            int newSets = (int)player1SetsField.GetValue(page);
            int player1Games = (int)player1GamesField.GetValue(page);
            int player2Games = (int)player2GamesField.GetValue(page);

            Assert.Equal(initialSets + 1, newSets);
            Assert.Equal(0, player1Games);
            Assert.Equal(0, player2Games);
        }

        [Fact]
        public void ClearGames_Clicked_ShouldResetGames()
        {
            // Arrange
            var page = new EnterLiveScorePage();

            var player1GamesField = typeof(EnterLiveScorePage).GetField(
                "player1Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var player2GamesField = typeof(EnterLiveScorePage).GetField(
                "player2Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            // Set initial values
            player1GamesField.SetValue(page, 3);
            player2GamesField.SetValue(page, 2);

            // Act
            var methodInfo = typeof(EnterLiveScorePage).GetMethod(
                "ClearGames_Clicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            methodInfo.Invoke(page, new object[] { null, EventArgs.Empty });

            // Assert
            int player1Games = (int)player1GamesField.GetValue(page);
            int player2Games = (int)player2GamesField.GetValue(page);

            Assert.Equal(0, player1Games);
            Assert.Equal(0, player2Games);
        }

        [Fact]
        public void ClearSets_Clicked_ShouldResetSetsAndGames()
        {
            // Arrange
            var page = new EnterLiveScorePage();

            var player1SetsField = typeof(EnterLiveScorePage).GetField(
                "player1Sets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var player2SetsField = typeof(EnterLiveScorePage).GetField(
                "player2Sets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var player1GamesField = typeof(EnterLiveScorePage).GetField(
                "player1Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            var player2GamesField = typeof(EnterLiveScorePage).GetField(
                "player2Games",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );

            // Set initial values
            player1SetsField.SetValue(page, 1);
            player2SetsField.SetValue(page, 1);
            player1GamesField.SetValue(page, 3);
            player2GamesField.SetValue(page, 2);

            // Act
            var methodInfo = typeof(EnterLiveScorePage).GetMethod(
                "ClearSets_Clicked",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
            );
            methodInfo.Invoke(page, new object[] { null, EventArgs.Empty });

            // Assert
            int player1Sets = (int)player1SetsField.GetValue(page);
            int player2Sets = (int)player2SetsField.GetValue(page);
            int player1Games = (int)player1GamesField.GetValue(page);
            int player2Games = (int)player2GamesField.GetValue(page);

            Assert.Equal(0, player1Sets);
            Assert.Equal(0, player2Sets);
            Assert.Equal(0, player1Games);
            Assert.Equal(0, player2Games);
        }

        // [Fact]
        // public void SendScore_ShouldFormatMessageCorrectly()
        // {
        //     // Arrange
        //     var page = new EnterLiveScorePage();

        //     // Set up test data
        //     var matchIdField = typeof(EnterLiveScorePage).GetField(
        //         "_matchId",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );
        //     var player1SetsField = typeof(EnterLiveScorePage).GetField(
        //         "player1Sets",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );
        //     var player2SetsField = typeof(EnterLiveScorePage).GetField(
        //         "player2Sets",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );
        //     var player1GamesField = typeof(EnterLiveScorePage).GetField(
        //         "player1Games",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );
        //     var player2GamesField = typeof(EnterLiveScorePage).GetField(
        //         "player2Games",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );
        //     var webSocketServiceField = typeof(EnterLiveScorePage).GetField(
        //         "_webSocketService",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );
        //     var isWebSocketConnectedField = typeof(EnterLiveScorePage).GetField(
        //         "_isWebSocketConnected",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );

        //     // Set test values
        //     matchIdField.SetValue(page, 123);
        //     player1SetsField.SetValue(page, 1);
        //     player2SetsField.SetValue(page, 0);
        //     player1GamesField.SetValue(page, 3);
        //     player2GamesField.SetValue(page, 2);
        //     webSocketServiceField.SetValue(page, _mockWebSocketService.Object);
        //     isWebSocketConnectedField.SetValue(page, true);

        //     // Expected message format based on the implementation
        //     string expectedMessage = "123,Set,10,Games,11,11,11,01,01,00";

        //     // Setup mock
        //     _mockWebSocketService
        //         .Setup(ws => ws.SendAsync(It.IsAny<string>()))
        //         .Returns(Task.CompletedTask)
        //         .Callback<string>(message =>
        //         {
        //             // Assert the message format inside the callback
        //             Assert.Equal(expectedMessage, message);
        //         });

        //     // Act
        //     var sendScoreMethod = typeof(EnterLiveScorePage).GetMethod(
        //         "SendScore",
        //         System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
        //     );
        //     sendScoreMethod.Invoke(page, null);

        //     // Assert
        //     _mockWebSocketService.Verify(ws => ws.SendAsync(It.IsAny<string>()), Times.Once);
        // }
    }
}
