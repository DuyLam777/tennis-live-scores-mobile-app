using System.Collections.ObjectModel;
using System.Text.Json;
using TennisApp.Models;
using TennisApp.Services;

namespace TennisApp.Services
{
    public class CourtAvailabilityService
    {
        private readonly WebSocketService _webSocketService;
        private readonly string _websocketUrl;
        private volatile bool _isListening = false;
        private CancellationTokenSource? _listeningCts;
        private readonly object _syncLock = new object();

        // Event to notify subscribers when court availability changes
        public event EventHandler<List<CourtItem>>? CourtAvailabilityChanged;

        public CourtAvailabilityService(WebSocketService webSocketService, string websocketUrl)
        {
            _webSocketService = webSocketService;
            _websocketUrl = websocketUrl;
        }

        public async Task StartListeningForCourtUpdatesAsync()
        {
            CancellationTokenSource? tokenSource = null;

            lock (_syncLock)
            {
                // If already listening, just return
                if (_isListening)
                {
                    Console.WriteLine("Already listening for court updates, ignoring request");
                    return;
                }

                // Create a new token source if needed
                if (_listeningCts == null || _listeningCts.IsCancellationRequested)
                {
                    _listeningCts = new CancellationTokenSource();
                }

                tokenSource = _listeningCts;
                _isListening = true;
                Console.WriteLine("Setting listening state to true");
            }

            try
            {
                // Connect to the WebSocket server if not already connected
                if (!_webSocketService.IsConnected)
                {
                    Console.WriteLine("Connecting to WebSocket server...");
                    await _webSocketService.ConnectAsync(_websocketUrl);
                    Console.WriteLine("Connected to WebSocket server");
                }
                else
                {
                    Console.WriteLine("Already connected to WebSocket server");
                }

                // Subscribe to court updates
                Console.WriteLine("Subscribing to court availability updates...");
                await _webSocketService.SendAsync(
                    JsonSerializer.Serialize(
                        new WebSocketRequest { Action = "subscribe", Topic = "court_availability" }
                    )
                );
                Console.WriteLine("Subscribed to court availability updates");

                // Start listening for messages in a background task
                var token = tokenSource.Token;
                _ = Task.Run(
                    async () =>
                    {
                        Console.WriteLine("Starting background listener task");
                        await ListenForUpdatesAsync(token);
                    },
                    CancellationToken.None
                );
            }
            catch (Exception ex)
            {
                lock (_syncLock)
                {
                    _isListening = false;
                }
                Console.WriteLine($"Error starting court updates: {ex.Message}");
                throw;
            }
        }

        public async Task StopListeningForCourtUpdatesAsync()
        {
            CancellationTokenSource? tokenToCancel = null;

            lock (_syncLock)
            {
                if (!_isListening)
                {
                    Console.WriteLine("Not listening for court updates, nothing to stop");
                    return;
                }

                tokenToCancel = _listeningCts;
                _isListening = false;
                Console.WriteLine("Setting listening state to false");
            }

            try
            {
                // Cancel the listening task
                if (tokenToCancel != null && !tokenToCancel.IsCancellationRequested)
                {
                    Console.WriteLine("Cancelling listener token");
                    tokenToCancel.Cancel();
                }

                // Unsubscribe from court updates if the connection is still open
                if (_webSocketService.IsConnected)
                {
                    Console.WriteLine("Unsubscribing from court updates...");
                    await _webSocketService.SendAsync(
                        JsonSerializer.Serialize(
                            new WebSocketRequest
                            {
                                Action = "unsubscribe",
                                Topic = "court_availability",
                            }
                        )
                    );
                    Console.WriteLine("Unsubscribed from court updates");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping court updates: {ex.Message}");
            }
        }

        private async Task ListenForUpdatesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && _webSocketService.IsConnected)
                {
                    // Receive message from WebSocket
                    string message = await _webSocketService.ReceiveAsync();

                    // Process the message
                    ProcessCourtUpdateMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                Console.WriteLine("WebSocket listener was cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in WebSocket listener: {ex.Message}");

                // Try to reconnect after a short delay if not canceled
                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(5000, CancellationToken.None);
                    _ = Task.Run(async () => await ReconnectAsync());
                }
            }
            finally
            {
                lock (_syncLock)
                {
                    _isListening = false;
                    Console.WriteLine(
                        "WebSocket listener has stopped, setting _isListening to false"
                    );
                }
            }
        }

        private async Task ReconnectAsync()
        {
            try
            {
                Console.WriteLine("Attempting to reconnect...");

                // Close existing connection if any
                await _webSocketService.CloseAsync();

                // Small delay to allow proper closure
                await Task.Delay(1000);

                // Try to restart listening
                await StartListeningForCourtUpdatesAsync();
                Console.WriteLine("Reconnected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to reconnect: {ex.Message}");
            }
        }

        private void ProcessCourtUpdateMessage(string message)
        {
            try
            {
                // Parse the WebSocket message
                var messageObj = JsonSerializer.Deserialize<WebSocketMessage>(message);

                if (messageObj?.Type == "court_availability" && messageObj.Data != null)
                {
                    Console.WriteLine($"Raw data: {messageObj.Data.ToString()}");

                    // Use specific options for deserializing courts
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip,
                    };

                    // Parse the courts data
                    var courts = JsonSerializer.Deserialize<List<CourtItem>>(
                        messageObj.Data.ToString() ?? "[]",
                        options
                    );

                    if (courts != null)
                    {
                        Console.WriteLine($"Parsed {courts.Count} courts successfully");
                        foreach (var court in courts)
                        {
                            Console.WriteLine(
                                $"Parsed court: {court.Id}, {court.Name}, Available: {court.IsAvailable}"
                            );
                        }

                        // Notify subscribers on the main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CourtAvailabilityChanged?.Invoke(this, courts);
                        });
                    }
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Error parsing WebSocket message: {ex.Message}");
                Console.WriteLine($"Message content: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing court update: {ex.Message}");
            }
        }
    }

    public class WebSocketRequest
    {
        public string Action { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
    }

    // WebSocket message structure
    public class WebSocketMessage
    {
        public string Type { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
