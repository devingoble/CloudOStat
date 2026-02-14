# CloudOStat Lode Index

## Core Documentation
- [summary.md](summary.md) – One-paragraph living project snapshot
- [terminology.md](terminology.md) – Domain language, concepts, abbreviations
- [practices.md](practices.md) – Architecture, DI patterns, multi-host strategy

## UI & Frontend
- [ui/architecture.md](ui/architecture.md) – Component hierarchy, responsive design, theming, form factor logic
  - Shared Razor components (MainLayout, NavMenu, BottomNav, Pages)
  - CSS strategy (global + scoped)
  - MudBlazor theme integration
  - Platform abstraction via IFormFactor

## Hardware & Control
- [hardware/sensors-and-control.md](hardware/sensors-and-control.md) – Meadow F7, MAX31855 thermocouples, relay, LCD, RGB LED
  - IHardwarePackage abstraction
  - SPI communication, digital I/O
  - Sensor reading & relay control patterns

## Development & Operations
- [dev/build-deploy.md](dev/build-deploy.md) – Build process, AppHost, testing, package management, CI/CD
  - Local dev setup (AppHost orchestration)
  - Publish workflows (MAUI, Blazor, Docker)
  - Troubleshooting guide

---

## Quick Navigation by Role

**Frontend Developer**:
1. [practices.md](practices.md) – Understand multi-host DI, service registration
2. [ui/architecture.md](ui/architecture.md) – Component model, responsive logic, theming
3. Project structure in [practices.md](practices.md)

**Backend/Hardware Developer**:
1. [practices.md](practices.md) – DI patterns, ServiceDefaults
2. [hardware/sensors-and-control.md](hardware/sensors-and-control.md) – Sensor integration, relay control
3. [dev/build-deploy.md](dev/build-deploy.md) – Device testing, build

**DevOps/Deployment**:
1. [dev/build-deploy.md](dev/build-deploy.md) – Build, publish, CI/CD
2. [practices.md](practices.md) – Multi-target strategy, package management
3. [terminology.md](terminology.md) – Naming conventions

**New Team Member**:
1. [summary.md](summary.md) – What is CloudOStat?
2. [terminology.md](terminology.md) – Domain language
3. [practices.md](practices.md) – Architecture overview

---

**Last Updated**: Session audit; Lode created and synchronized with codebase state.  
**Status**: Active; ready for ongoing reference and updates.
