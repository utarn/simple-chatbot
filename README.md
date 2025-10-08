# Development Environment Setup Script

This PowerShell script automates the installation of essential development tools on Windows systems.

## Tools Installed

1. .NET 8 SDK
2. Visual Studio Code
3. Git
4. Windows Terminal
5. PowerShell 7
6. PostgreSQL 17

## Requirements

- Windows Operating System
- PowerShell 5.0 or higher
- Internet connection

## Usage

### Run with default settings
```powershell
.\setup-dev-environment.ps1
```

### Run without administrator check
```powershell
.\setup-dev-environment.ps1 -SkipAdminCheck
```

### Force installation without prompts
```powershell
.\setup-dev-environment.ps1 -Force
```

## Administrator Rights

For best results, run this script as Administrator:
1. Right-click on PowerShell
2. Select "Run as Administrator"
3. Navigate to the script directory
4. Execute the script

If the script is not run as Administrator, it will prompt you to continue anyway or cancel the setup.

## How It Works

The script checks if each tool is already installed before attempting to install it. If a tool is found, it skips that installation.

For installation, the script attempts to use package managers in this order:
1. winget (Windows Package Manager)
2. Chocolatey
3. Direct download from official sources

If none of the package managers are available, it falls back to direct downloads.

## Notes

- The script will download installers to your system's TEMP directory and remove them after installation
- Some installations may require a system restart to complete properly
- PostgreSQL installation will require you to set up a password during the setup process if using direct download