# Setup Development Environment Script
# Installs .NET 8 SDK and necessary tools for development
# Compatible with PowerShell 5

#Requires -Version 5.0

 param(
     [switch]$Force,
     [switch]$SkipAdminCheck
 )

# Function to check if running as administrator
function Test-Administrator {
    $currentUser = [Security.Principal.WindowsPrincipal]([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentUser.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Function to install .NET 8 SDK
function Install-DotNet8SDK {
    Write-Host "Installing .NET 8 SDK..." -ForegroundColor Green
    
    # Check if .NET 8 SDK is already installed
    try {
        $dotnetInfo = dotnet --info 2>$null | Out-String
        if ($dotnetInfo -match "8\.0\.\d+") {
            Write-Host ".NET 8 SDK is already installed" -ForegroundColor Yellow
            return
        }
    }
    catch {
        Write-Host "dotnet command not found, proceeding with installation..." -ForegroundColor Yellow
    }
    
    # Download and install .NET 8 SDK
    try {
        # URL for .NET 8 SDK installer
        $downloadUrl = "https://download.visualstudio.microsoft.com/download/pr/73a9cb7d-65ce-4b03-80f8-9b723730f093/1050b8771123e439890e4d721619d957/dotnet-sdk-8.0.403-win-x64.exe"
        $installerPath = "$env:TEMP\dotnet-sdk-8.0.403-win-x64.exe"
        
        Write-Host "Downloading .NET 8 SDK installer..." -ForegroundColor Cyan
        Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -ErrorAction Stop
        
        Write-Host "Installing .NET 8 SDK..." -ForegroundColor Cyan
        Start-Process -FilePath $installerPath -ArgumentList "/install", "/quiet", "/norestart" -Wait -NoNewWindow
        
        Remove-Item $installerPath -Force
        Write-Host ".NET 8 SDK installation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install .NET 8 SDK: $($_.Exception.Message)"
    }
}

# Function to install Visual Studio Code
function Install-VSCode {
    Write-Host "Installing Visual Studio Code..." -ForegroundColor Green
    
    # Check if VS Code is already installed
    if (Get-Command code -ErrorAction SilentlyContinue) {
        Write-Host "Visual Studio Code is already installed" -ForegroundColor Yellow
        return
    }
    
    try {
        # Download and install VS Code using Chocolatey (if available) or directly
        if (Get-Command choco -ErrorAction SilentlyContinue) {
            Write-Host "Installing VS Code via Chocolatey..." -ForegroundColor Cyan
            choco install vscode -y --force
        }
        else {
            # URL for VS Code installer
            $downloadUrl = "https://code.visualstudio.com/sha/download?build=stable&os=win32-x64-user"
            $installerPath = "$env:TEMP\VSCodeSetup.exe"
            
            Write-Host "Downloading Visual Studio Code installer..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -ErrorAction Stop
            
            Write-Host "Installing Visual Studio Code..." -ForegroundColor Cyan
            Start-Process -FilePath $installerPath -ArgumentList "/VERYSILENT", "/NORESTART", "/MERGETASKS=!runcode" -Wait -NoNewWindow
            
            Remove-Item $installerPath -Force
        }
        Write-Host "Visual Studio Code installation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install Visual Studio Code: $($_.Exception.Message)"
    }
}

# Function to install Git
function Install-Git {
    Write-Host "Installing Git..." -ForegroundColor Green
    
    # Check if Git is already installed
    if (Get-Command git -ErrorAction SilentlyContinue) {
        Write-Host "Git is already installed" -ForegroundColor Yellow
        return
    }
    
    try {
        # Download and install Git using Chocolatey (if available) or directly
        if (Get-Command choco -ErrorAction SilentlyContinue) {
            Write-Host "Installing Git via Chocolatey..." -ForegroundColor Cyan
            choco install git -y --force
        }
        else {
            # URL for Git installer
            $downloadUrl = "https://github.com/git-for-windows/git/releases/download/v2.46.0.windows.1/Git-2.46.0-64-bit.exe"
            $installerPath = "$env:TEMP\GitSetup.exe"
            
            Write-Host "Downloading Git installer..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -ErrorAction Stop
            
            Write-Host "Installing Git..." -ForegroundColor Cyan
            Start-Process -FilePath $installerPath -ArgumentList "/VERYSILENT", "/NORESTART" -Wait -NoNewWindow
            
            Remove-Item $installerPath -Force
        }
        Write-Host "Git installation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install Git: $($_.Exception.Message)"
    }
}

# Function to install Windows Terminal
function Install-WindowsTerminal {
    Write-Host "Installing Windows Terminal..." -ForegroundColor Green
    
    # Check if Windows Terminal is already installed
    try {
        $terminalPackage = Get-AppxPackage -Name "Microsoft.WindowsTerminal" -ErrorAction SilentlyContinue
        if ($terminalPackage) {
            Write-Host "Windows Terminal is already installed (version: $($terminalPackage.Version))" -ForegroundColor Yellow
            return
        }
    }
    catch {
        Write-Host "Unable to check Windows Terminal installation status" -ForegroundColor Yellow
    }
    
    try {
        # Install Windows Terminal from Microsoft Store (using winget if available)
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            Write-Host "Installing Windows Terminal via winget..." -ForegroundColor Cyan
            winget install --id=Microsoft.WindowsTerminal -e --source=winget --accept-source-agreements --accept-package-agreements --force
        }
        else {
            Write-Host "winget not found. Please install Windows Terminal manually from the Microsoft Store." -ForegroundColor Yellow
        }
        Write-Host "Windows Terminal installation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install Windows Terminal: $($_.Exception.Message)"
    }
}

# Function to install PowerShell 7
function Install-PowerShell7 {
    Write-Host "Installing PowerShell 7..." -ForegroundColor Green
    
    # Check if PowerShell 7 is already installed
    if (Get-Command pwsh -ErrorAction SilentlyContinue) {
        Write-Host "PowerShell 7 is already installed" -ForegroundColor Yellow
        return
    }
    
    try {
        # Download and install PowerShell 7 using winget (if available) or directly
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            Write-Host "Installing PowerShell 7 via winget..." -ForegroundColor Cyan
            winget install --id=Microsoft.PowerShell -e --source=winget --accept-source-agreements --accept-package-agreements --force
        }
        else {
            # URL for PowerShell 7 installer
            $downloadUrl = "https://github.com/PowerShell/PowerShell/releases/download/v7.4.5/PowerShell-7.4.5-win-x64.msi"
            $installerPath = "$env:TEMP\PowerShell7Setup.msi"
            
            Write-Host "Downloading PowerShell 7 installer..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -ErrorAction Stop
            
            Write-Host "Installing PowerShell 7..." -ForegroundColor Cyan
            Start-Process -FilePath "msiexec.exe" -ArgumentList "/i", "$installerPath", "/quiet", "/norestart" -Wait -NoNewWindow
            
            Remove-Item $installerPath -Force
        }
        Write-Host "PowerShell 7 installation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install PowerShell 7: $($_.Exception.Message)"
    }
}

# Function to install PostgreSQL 17
function Install-PostgreSQL17 {
    Write-Host "Installing PostgreSQL 17..." -ForegroundColor Green
    
    # Check if PostgreSQL is already installed
    try {
        $pgServices = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue
        if ($pgServices) {
            Write-Host "PostgreSQL is already installed (services found: $($pgServices.Name -join ', ')))" -ForegroundColor Yellow
            return
        }
    }
    catch {
        Write-Host "Unable to check PostgreSQL installation status" -ForegroundColor Yellow
    }
    
    try {
        # Download and install PostgreSQL 17 using winget (if available) or directly
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            Write-Host "Installing PostgreSQL 17 via winget..." -ForegroundColor Cyan
            winget install --id=PostgreSQL.PostgreSQL -e --source=winget --accept-source-agreements --accept-package-agreements --force
        }
        else {
            # URL for PostgreSQL 17 installer
            $downloadUrl = "https://get.enterprisedb.com/postgresql/postgresql-17.0-1-windows-x64.exe"
            $installerPath = "$env:TEMP\PostgreSQLSetup.exe"
            
            Write-Host "Downloading PostgreSQL 17 installer..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -ErrorAction Stop
            
            Write-Host "Installing PostgreSQL 17..." -ForegroundColor Cyan
            Start-Process -FilePath $installerPath -ArgumentList "--mode", "unattended", "--unattendedmodeui", "none" -Wait -NoNewWindow
            
            Remove-Item $installerPath -Force
        }
        Write-Host "PostgreSQL 17 installation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install PostgreSQL 17: $($_.Exception.Message)"
    }
}

# Main execution
Write-Host "Starting Development Environment Setup..." -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

# Check if running as administrator
if (!$SkipAdminCheck -and !(Test-Administrator)) {
    Write-Warning "This script should be run as Administrator for best results."
    Write-Host "Please right-click PowerShell and select 'Run as Administrator'." -ForegroundColor Yellow
    if (!$Force) {
        $response = Read-Host "Continue anyway? (y/n)"
        if ($response -ne 'y') {
            Write-Host "Setup cancelled." -ForegroundColor Red
            return
        }
    }
}

# Install all required tools
Install-DotNet8SDK
Install-VSCode
Install-Git
Install-WindowsTerminal
Install-PowerShell7
Install-PostgreSQL17

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Development Environment Setup Complete!" -ForegroundColor Magenta
Write-Host "Please restart your terminal to ensure all changes take effect." -ForegroundColor Cyan
Write-Host "You may need to restart your computer for all changes to be fully applied." -ForegroundColor Cyan