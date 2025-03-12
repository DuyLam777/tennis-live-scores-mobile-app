namespace TennisApp.Config
{
    public static class AppConfig
    {
        // Server IP address (Laptop IP address)
        public static string ServerIP = "192.168.195.20";

        // WebSocket port
        public static int WebSocketPort = 5020;

        public static string GetWebSocketUrl()
        {
            return $"ws://{ServerIP}:{WebSocketPort}/ws";
        }
    }
}
