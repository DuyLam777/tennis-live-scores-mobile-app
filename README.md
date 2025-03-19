# Tennis Live Scores - Mobile App

A .NET MAUI mobile application for tracking and sharing live scores from tennis matches. This app is part of a larger platform designed to improve the tournament experience by providing real-time match updates.

## Overview

During tennis tournaments, matches often extend beyond their scheduled time slots, causing frustration for waiting players. This platform addresses this issue by providing a way to track and share live scores, helping players better manage their waiting time.

The system consists of three components:

- **Web Application**: Displays live scores for ongoing matches
- **Mobile Application** (this project): For entering scores and connecting to physical scoreboards
- **Hardware Client**: Physical scoreboards that can be connected to the mobile app via Bluetooth

## Features

### Match Management

- View ongoing matches and court availability
- Create new matches with player, court, and scoreboard selection
- Share match links via messaging platforms

### Score Entry

- Enter scores manually for matches
- Connect to physical scoreboards via Bluetooth
- Real-time synchronization with the server

### Court Management

- View court availability in real-time
- Track which courts are in use
- See match details for each court

### Bluetooth Connectivity

- Scan and connect to Bluetooth scoreboards
- Automatically read and transmit scores from physical scoreboards
- Monitor scoreboard battery levels

## Technical Details

### Technology Stack

- **.NET MAUI**: Cross-platform framework supporting Android
- **WebSockets**: For real-time data synchronization
- **Bluetooth LE**: For connecting to physical scoreboards

### Architecture

- **MVVM Pattern**: Using the CommunityToolkit.Mvvm library
- **Dependency Injection**: For service management and testability
- **Real-time Communication**: WebSockets for server communication
- **Responsive UI**: Adapts to different device sizes and orientations

## Getting Started

### Prerequisites

- Visual Studio 2022 or later with .NET MAUI workload
- Android SDK (for Android development)

### Configuration

#### Server Connection

The app connects to a server specified in the `AppConfig.cs` file. Update the `ServerIP` and `ServerPort` values to match your server configuration:

```csharp
public static class AppConfig
{
    // Server IP address
    public static string ServerIP = "192.168.0.174";

    // WebSocket port
    public static int ServerPort = 5020;
    
    // ...
}
```

#### Network Security Configuration

The network configuration needs to be set up in the `network_security_config.xml` file to allow communication between the mobile app and the server.

```xml
<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
    <domain-config cleartextTrafficPermitted="true">
        <domain includeSubdomains="true">192.168.0.174</domain>
    </domain-config>
</network-security-config>
```

Make sure to update the domain value with your server's IP address. This file is located at `TennisApp/Platforms/Android/Resources/xml/network_security_config.xml`.

### Building the App

1. Clone the repository
2. Open the solution in Visual Studio
3. Select your target platform (Android, iOS, Windows, or MacCatalyst)
4. Build and run the application

## Usage Guide

### Creating a Match

1. From the home screen, tap "Start New Match"
2. Select the match time, court, and players
3. Optionally select a physical scoreboard
4. Tap "Create Match" to finalize

### Entering Scores

1. From the home screen, tap "Connect Scoreboard"
2. Select a match from the list
3. Choose "Enter Scores Manually" or "Connect to Scoreboard"
4. For manual entry, use the buttons to update sets and games
5. For scoreboard connection, select your Bluetooth device and follow the prompts

### Viewing Court Availability

The home screen displays court availability in real-time, showing which courts are free and which are in use.

## Permissions

The app requires the following permissions:

- **Internet**: For WebSocket communication with the server
- **Bluetooth**: For connecting to physical scoreboards
- **Location**: Required for Bluetooth scanning on Android

## Development Notes

### Project Structure

- **Models**: Data objects representing matches, courts, players, etc.
- **ViewModels**: MVVM implementation for UI logic
- **Views**: XAML UI components
- **Services**: WebSocket and Bluetooth communication
- **Utils**: Helper classes for various functionalities

### WebSocket Communication

The app uses WebSockets for real-time communication with the server. The `WebSocketService` class handles connection, message sending, and receiving.

### Bluetooth Integration

The `BluetoothConnectionPage` and `BluetoothMessagePage` handle device discovery, connection, and data transmission from physical scoreboards.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

