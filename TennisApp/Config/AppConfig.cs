namespace TennisApp.Config
{
    public static class AppConfig
    {
        // Server IP address (Laptop IP address)
        public static string ServerIP = "192.168.0.174";

        // WebSocket port
        public static int ServerPort = 5020;

        public static string GetWebSocketUrl()
        {
            return $"ws://{ServerIP}:{ServerPort}/ws";
        }

        public static string GetApiUrl()
        {
            return $"http://{ServerIP}:{ServerPort}";
        }
    }
}
