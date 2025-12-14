# LG Fan Tray

A lightweight Windows system tray application for controlling fan speeds on LG laptops via EC (Embedded Controller) register manipulation.

## Features

- **System Tray Control**: Quick access to fan modes from the system tray
- **Global Hotkeys**: Change fan speed from anywhere using keyboard shortcuts
- **Command Line Interface**: Control fan speed via command line arguments
- **Single Instance**: Multiple launches send commands to the running instance
- **Run at Startup**: Optional Windows Task Scheduler integration for auto-start

## Fan Modes

| Mode | EC Value | Description |
|------|----------|-------------|
| Low | 0x11 | Quietest, minimal cooling |
| Normal | 0x00 | Default/balanced |
| High | 0x22 | Increased cooling |
| Max | 0x44 | Maximum fan speed |

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+Alt+1 | Set Low mode |
| Ctrl+Alt+2 | Set Normal mode |
| Ctrl+Alt+3 | Set High mode |
| Ctrl+Alt+4 | Set Max mode |

## Command Line Usage

```bash
# Set fan mode directly
LgFanTray.exe -1        # Low
LgFanTray.exe -low      # Low
LgFanTray.exe -2        # Normal
LgFanTray.exe -normal   # Normal
LgFanTray.exe -3        # High
LgFanTray.exe -high     # High
LgFanTray.exe -4        # Max
LgFanTray.exe -max      # Max

# Launch without setting a mode (just start tray app)
LgFanTray.exe
```

If the app is already running, command line arguments will send the command to the running instance via named pipe (no duplicate windows).

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

LG Fan Tray uses `ec-probe.exe` (from the NBFC project) to read/write EC register `0xCF`, which controls the fan mode on LG laptops. The app wraps ec-probe in a user-friendly tray interface with hotkey support.

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
│       ├── FanMode.cs              # Fan mode enum
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

## Credits

- Based on [NBFC (NoteBook Fan Control)](https://github.com/hirschmann/nbfc) by hirschmann
- Uses ec-probe for EC register access

## License

See the NBFC project for license information regarding ec-probe and related components.
