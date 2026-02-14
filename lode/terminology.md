# CloudOStat Terminology

## Core Domain Terms
- **Smoker/Grill**: The outdoor device being monitored and controlled; CloudOStat was originally designed for smokers but applies to any device needing temperature monitoring.
- **Pit**: Often refers to the smoker chamber itself.
- **Probe / Sensor**: Thermocouples reading temperature; three total (air + meat1 + meat2).
- **Stall**: Temperature plateau during cooking; tracked via Warning status color.
- **Pit Master**: The user operating the smoker; inspired naming for theme variants.

## Hardware Layer
- **Meadow F7 Feather**: Microcontroller board running the hardware package.
- **MAX31855**: K-type thermocouple amplifier driver (temperature sensors).
- **Heater Relay**: Digital output controlling auxiliary heating element.
- **Character LCD**: 4x20 text display attached to device (local real-time feedback).
- **RGB LED**: Onboard status indicator.

## UI / Platform Terms
- **MAUI**: .NET Multi-platform App UI (desktop/mobile client).
- **Blazor**: Web-based UI framework; hybrid server+WebAssembly architecture.
- **Shared**: `CloudOStat.App.Shared` project containing cross-platform components, layouts, pages, themes.
- **MudBlazor**: Blazor component library providing Material Design UI and theming.
- **NavigationService**: Centralized route management across all platform hosts.

## Architecture / Project Structure
- **AppHost**: Orchestrator project (uses .NET Aspire for local multi-project orchestration).
- **ServiceDefaults**: Common service registration and middleware (HTTP resilience, telemetry).
- **Common / LocalHardware**: Hardware abstraction layer (interfaces for sensors, displays, relays).
- **Device**: Platform-specific device integration (e.g., Meadow F7 initialization).

## Theme / Color Concepts
- **Primary**: Main UI accent color.
- **Secondary**: Secondary/neutral tones.
- **Accent**: Highlight/tertiary color.
- **Background**: Page/container background.
- **Heating / Cooling / OnTemp**: State-specific colors reflecting temperature direction.
- **Warning / Error**: Alert severity indicators.

## Navigation
- **PrimaryNav**: Dashboard-focused routes (Home, Dashboard, Settings).
- **SecondaryNav**: Utility routes (Counter demo, Weather, About).
- **Route**: A page endpoint (e.g., `/dashboard`, `/settings`).
