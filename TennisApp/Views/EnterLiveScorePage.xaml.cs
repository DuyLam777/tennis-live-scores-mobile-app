using System;
using TennisApp.Services;
using Microsoft.Maui.Controls;
using TennisApp.Config;

namespace TennisApp.Views
{
    public partial class EnterLiveScorePage : ContentPage
    {
        private int player1Sets = 0;
        private int player2Sets = 0;
        private int player1Games = 0;
        private int player2Games = 0;

        private WebSocketService? _webSocketService;
        private bool _isWebSocketConnected = false;

        public EnterLiveScorePage()
        {
            InitializeComponent();
            UpdateScoreDisplay();
            ConnectWebSocket();
        }

        private async void ConnectWebSocket()
        {
            try
            {
                _webSocketService = new WebSocketService();
                string webSocketUrl = AppConfig.GetWebSocketUrl();
                await _webSocketService.ConnectAsync(webSocketUrl);
                _isWebSocketConnected = true;
                await DisplayAlert("Connected", "WebSocket connected successfully", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"WebSocket connection failed: {ex.Message}", "OK");
            }
        }

        private void UpdateScoreDisplay()
        {
            Player1SetsLabel.Text = $"{player1Sets}";
            Player2SetsLabel.Text = $"{player2Sets}";
            Player1GamesLabel.Text = $"{player1Games}";
            Player2GamesLabel.Text = $"{player2Games}";
        }

        // New helper method that sends the score via WebSocket
        private async void SendScore()
        {
            if (_isWebSocketConnected && _webSocketService != null)
            {
                try
                {
                    // Build the message. For example:
                    // "match id here,Set,11,Games,11,11,10,10,10,00"
                    string message = $"1,Set,{player1Sets}{player2Sets},Games,";
                    for (int i = 1; i <= 6; i++)
                    {
                        string segment;
                        if (player1Games >= i && player2Games >= i)
                        {
                            segment = "11";
                        }
                        else if (player1Games >= i)
                        {
                            segment = "10";
                        }
                        else if (player2Games >= i)
                        {
                            segment = "01";
                        }
                        else
                        {
                            segment = "00";
                        }
                        message += segment + (i < 6 ? "," : "");
                    }
                    await _webSocketService.SendAsync(message);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Failed to send score: {ex.Message}", "OK");
                }
            }
        }

        // Each event handler now updates the display and then sends the score automatically.

        private void AddGameP1_Clicked(object sender, EventArgs e)
        {
            player1Games++;
            UpdateScoreDisplay();
            SendScore();
        }

        private void AddGameP2_Clicked(object sender, EventArgs e)
        {
            player2Games++;
            UpdateScoreDisplay();
            SendScore();
        }

        private void AddSetP1_Clicked(object sender, EventArgs e)
        {
            player1Sets++;
            // Reset games when a set is won
            player1Games = 0;
            player2Games = 0;
            UpdateScoreDisplay();
            SendScore();
        }

        private void AddSetP2_Clicked(object sender, EventArgs e)
        {
            player2Sets++;
            player1Games = 0;
            player2Games = 0;
            UpdateScoreDisplay();
            SendScore();
        }

        private void ClearGames_Clicked(object sender, EventArgs e)
        {
            player1Games = 0;
            player2Games = 0;
            UpdateScoreDisplay();
            SendScore();
        }

        private void ClearSets_Clicked(object sender, EventArgs e)
        {
            player1Sets = 0;
            player2Sets = 0;
            player1Games = 0;
            player2Games = 0;
            UpdateScoreDisplay();
            SendScore();
        }

        // Override OnDisappearing to close the WebSocket connection
        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            if (_webSocketService != null)
            {
                try
                {
                    await _webSocketService.CloseAsync();
                    _isWebSocketConnected = false;
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", $"Error closing WebSocket: {ex.Message}", "OK");
                }
            }
        }
    }
}
