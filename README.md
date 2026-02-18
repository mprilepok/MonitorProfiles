# MonitorProfiles

A Windows command-line tool to save and restore display/monitor arrangements. Useful when switching between setups (e.g., docked with external monitors vs. laptop only).

## Features

- Save current display arrangement (positions, resolution, primary monitor) as a named profile
- Restore a saved profile with one command
- Interactive mode — run without arguments to pick a profile
- Zero dependencies — uses Win32 CCD API directly via P/Invoke
- Profiles stored as JSON in `%APPDATA%\MonitorProfiles\`

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Build

```
dotnet build
```

## Usage

### Interactive mode

Run without arguments to see saved profiles and pick one to load:

```
MonitorProfiles
```

```
MonitorProfiles - Display Setup Profile Manager

Select a profile to load:

  1. docked
  2. laptop-only
  3. Exit

>
```

### CLI commands

| Command | Aliases | Description |
|---------|---------|-------------|
| `save <name>` | `-s`, `--save` | Save current display arrangement |
| `load <name>` | `-l`, `--load` | Apply a saved profile |
| `list` | `--list` | List saved profiles |
| `delete <name>` | `-d`, `--delete` | Delete a saved profile |
| `help` | `-h`, `--help` | Show usage information |

### Example workflow

```bash
# Dock your laptop, arrange monitors how you like, then save
MonitorProfiles save docked

# Undock and use laptop screen only, save that too
MonitorProfiles save laptop-only

# Next time you dock, restore your layout
MonitorProfiles load docked
```

## How it works

- Enumerates displays using `EnumDisplayDevices` and `EnumDisplaySettings`
- Applies profiles using `ChangeDisplaySettingsEx` with staged updates
- Matches displays across sessions by hardware Device ID
