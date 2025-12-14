# LG Fan Tray

A lightweight Windows system tray application for controlling fan speeds and keyboard backlight on LG laptops via EC (Embedded Controller) register manipulation.

## Features

- **System Tray Control**: Quick access to fan modes and keyboard backlight from the system tray
- **Global Hotkeys**: Change fan speed and backlight from anywhere using keyboard shortcuts
- **Command Line Interface**: Control fan speed and backlight via command line arguments
- **Single Instance**: Multiple launches send commands to the running instance
- **Run at Startup**: Optional Windows Task Scheduler integration for auto-start

## Fan Modes

| Mode | EC Register | EC Value | Description |
|------|-------------|----------|-------------|
| Low | 0xCF | 0x11 | Quietest, minimal cooling |
| Normal | 0xCF | 0x00 | Default/balanced |
| High | 0xCF | 0x22 | Increased cooling |
| Max | 0xCF | 0x44 | Maximum fan speed |

## Keyboard Backlight

| Level | EC Register | EC Value | Description |
|-------|-------------|----------|-------------|
| Off | 0x72 | 0x80 | Backlight disabled |
| Low | 0x72 | 0xA2 | Dim backlight |
| High | 0x72 | 0xA4 | Bright backlight |

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Alt+1 | Fan: Low |
| Ctrl+Alt+2 | Fan: Normal |
| Ctrl+Alt+3 | Fan: High |
| Ctrl+Alt+4 | Fan: Max |
| Ctrl+Alt+5 | Backlight: Off |
| Ctrl+Alt+6 | Backlight: Low |
| Ctrl+Alt+7 | Backlight: High |

## Command Line Usage

```bash
# Fan modes
LgFanTray.exe -1            # Fan Low
LgFanTray.exe -2            # Fan Normal
LgFanTray.exe -3            # Fan High
LgFanTray.exe -4            # Fan Max
LgFanTray.exe -fanlow       # Fan Low
LgFanTray.exe -fannormal    # Fan Normal
LgFanTray.exe -fanhigh      # Fan High
LgFanTray.exe -fanmax       # Fan Max

# Keyboard backlight
LgFanTray.exe -5            # Backlight Off
LgFanTray.exe -6            # Backlight Low
LgFanTray.exe -7            # Backlight High
LgFanTray.exe -lightoff     # Backlight Off
LgFanTray.exe -lightlow     # Backlight Low
LgFanTray.exe -lighthigh    # Backlight High

# Launch without setting a mode (just start tray app)
LgFanTray.exe
```

If the app is already running, fan mode commands will send the command to the running instance via named pipe (no duplicate windows). Backlight commands execute directly.

## Installation

### Using the Installer

1. Download `LgFanTraySetup.msi` from the releases
2. Run the installer
3. Optionally check "Run at startup" to launch automatically at logon
4. Click "Launch LG Fan Tray" on the final screen to start immediately

### Manual Installation

1. Copy all files from `deps/` to your desired location
2. Copy `LgFanTray.exe` to the same location
3. Run `LgFanTray.exe` as Administrator

## Requirements

- Windows 10/11
- .NET Framework 4.8
- Administrator privileges (required for EC access)

## How It Works

LG Fan Tray uses `ec-probe.exe` (from the NBFC project) to read/write EC registers:
- **0xCF** - Fan mode control
- **0x72** - Keyboard backlight control

The EC register values were discovered through trial and error using `ec-probe dump` and `ec-probe monitor` while toggling settings on an LG Gram laptop.

### Dependencies (in deps/)

| File | Purpose |
|------|---------|
| ec-probe.exe | EC register read/write utility |
| nbfc.exe | CLI helper (required by ec-probe) |
| clipr.dll | Command line parsing |
| NLog.dll | Logging framework |
| StagWare.FanControl.dll | Fan control core |
| StagWare.FanControl.Configurations.dll | Configuration handling |
| StagWare.BiosInfo.dll | BIOS information |
| System.IO.Abstractions.dll | File system abstraction |

### Plugins (in deps/Plugins/)

| File | Purpose |
|------|---------|
| StagWare.Plugins.ECWindows.dll | Windows EC access plugin |
| StagWare.Hardware.dll | Hardware abstraction |
| StagWare.Hardware.LPC.dll | LPC port I/O |
| OpenHardwareMonitorLib.dll | Hardware monitoring |
| WinRing0x64.sys | Kernel driver for EC access |

## Building from Source

### Prerequisites

- Visual Studio 2019+ or .NET SDK
- WiX Toolset v6 (for installer)

### Build Steps

```bash
# Build the application
dotnet build src/LgFanTray/LgFanTray.csproj -c Release

# Build the installer (requires WiX)
dotnet build setup/LgFanTraySetup.wixproj -c Release
```

The installer will be output to `setup/bin/Release/LgFanTraySetup.msi`.

## Project Structure

```
LgFanTray/
├── src/
│   └── LgFanTray/          # Main application source
│       ├── Program.cs              # Entry point, single-instance, CLI parsing
│       ├── TrayApplicationContext.cs # Tray icon, menu, hotkeys, IPC
│       ├── EcProbeClient.cs        # ec-probe.exe wrapper
│       ├── FanMode.cs              # Fan mode enum (0xCF values)
│       ├── BacklightLevel.cs       # Backlight level enum (0x72 values)
│       ├── HotkeyManager.cs        # Global hotkey registration
│       └── AppSettings.cs          # Settings definitions
├── setup/                  # WiX installer project
│   ├── Product.wxs
│   ├── LgFanTrayUI_InstallDir.wxs
│   └── LgFanTraySetup.wixproj
├── deps/                   # ec-probe and dependencies
│   ├── Plugins/
│   └── ...
└── README.md
```

## Discovering EC Registers

To find additional EC-controlled features on your LG laptop:

```bash
# Dump all EC registers
ec-probe dump

# Monitor for changes (toggle a feature while monitoring)
ec-probe monitor -i 2 -t 30

# Read a specific register
ec-probe read 0xCF

# Write to a register (be careful!)
ec-probe write 0xCF 0x44 -v
```

## Credits

- Based on [NBFC (NoteBook Fan Control)](https://github.com/hirschmann/nbfc) by hirschmann
- Uses ec-probe for EC register access
- EC register values discovered by James DuRant

## License

See the NBFC project for license information regarding ec-probe and related components.
