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
        $downloadUrl = "https://builds.dotnet.microsoft.com/dotnet/Sdk/8.0.414/dotnet-sdk-8.0.414-win-x64.exe"
        $installerPath = "$env:TEMP\dotnet-sdk-8.0.414-win-x64.exe"
        
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

        # Install required VS Code extensions
        $extensions = @(
            "modelharbor.modelharbor-agent",
            "ms-dotnettools.vscode-dotnet-runtime",
            "formulahendry.dotnet",
            "ms-dotnettools.csharp",
            "ms-dotnettools.csdevkit",
            "ms-dotnettools.vscodeintellicode-csharp",
            "alexcvzz.vscode-sqlite"
        )

        foreach ($ext in $extensions) {
            Write-Host "Installing $ext extension..." -ForegroundColor Cyan
            try {
                code --install-extension $ext
                Write-Host "$ext extension installed successfully!" -ForegroundColor Green
            }
            catch {
                Write-Warning "Failed to install $ext extension: $($_.Exception.Message)"
            }
        }
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

# Function to install Docker Desktop
function Install-DockerDesktop {
    Write-Host "Installing Docker Desktop..." -ForegroundColor Green

    # Check if Docker Desktop is already installed
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "Docker Desktop is already installed" -ForegroundColor Yellow
        return
    }

    try {
        # Install Docker Desktop using winget (if available) or Chocolatey or directly
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            Write-Host "Installing Docker Desktop via winget..." -ForegroundColor Cyan
            winget install --id=Microsoft.DockerDesktop -e --source=winget --accept-source-agreements --accept-package-agreements --force
        }
        elseif (Get-Command choco -ErrorAction SilentlyContinue) {
            Write-Host "Installing Docker Desktop via Chocolatey..." -ForegroundColor Cyan
            choco install docker-desktop -y --force
        }
        else {
            # URL for Docker Desktop installer
            $downloadUrl = "https://desktop.docker.com/win/main/amd64/Docker%20Desktop%20Installer.exe"
            $installerPath = "$env:TEMP\DockerDesktopInstaller.exe"

            Write-Host "Downloading Docker Desktop installer..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -ErrorAction Stop

            Write-Host "Installing Docker Desktop..." -ForegroundColor Cyan
            Start-Process -FilePath $installerPath -ArgumentList "install", "--quiet" -Wait -NoNewWindow

            Remove-Item $installerPath -Force
        }
        Write-Host "Docker Desktop installation completed successfully!" -ForegroundColor Green

        # Configure Docker Desktop settings
        Write-Host "Configuring Docker Desktop settings..." -ForegroundColor Cyan
        try {
            $settingsPath = "$env:APPDATA\Docker Desktop\settings.json"
            if (Test-Path $settingsPath) {
                $settings = Get-Content $settingsPath | ConvertFrom-Json
                # Set memory to 2GB
                $settings.memoryMiB = 2048
                # Set CPUs to all available
                $cpuCount = (Get-CimInstance Win32_ComputerSystem).NumberOfLogicalProcessors
                $settings.cpus = $cpuCount
                # Set swap to 1GB
                $settings.swapMiB = 1024
                # Save back
                $settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
                Write-Host "Docker Desktop settings configured." -ForegroundColor Green
            } else {
                Write-Warning "Docker Desktop settings file not found."
            }
        } catch {
            Write-Warning "Failed to configure Docker Desktop settings: $($_.Exception.Message)"
        }

        # Enable Docker Desktop to start on Windows boot
        Write-Host "Enabling Docker Desktop to start on Windows boot..." -ForegroundColor Cyan
        try {
            $dockerPath = "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe"
            if (Test-Path $dockerPath) {
                reg add "HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "Docker Desktop" /t REG_SZ /d "\"$dockerPath\"" /f | Out-Null
                Write-Host "Docker Desktop will start on boot." -ForegroundColor Green
            } else {
                Write-Warning "Docker Desktop executable not found at expected path."
            }
        } catch {
            Write-Warning "Failed to enable Docker Desktop on boot: $($_.Exception.Message)"
        }

        # Start Docker Desktop (minimized to prevent opening dashboard)
        Write-Host "Starting Docker Desktop..." -ForegroundColor Cyan
        try {
            Start-Process -FilePath "$dockerPath" -ArgumentList "--minimized"
            Write-Host "Docker Desktop started (minimized)." -ForegroundColor Green
        } catch {
            Write-Warning "Failed to start Docker Desktop: $($_.Exception.Message)"
        }

        # Wait for Docker to be ready and pull image
        Write-Host "Waiting for Docker to be ready..." -ForegroundColor Cyan
        $maxRetries = 10
        $retryCount = 0
        $dockerReady = $false
        while ($retryCount -lt $maxRetries -and !$dockerReady) {
            try {
                docker ps 2>$null | Out-Null
                $dockerReady = $true
            } catch {
                $retryCount++
                Start-Sleep -Seconds 5
            }
        }
        if (!$dockerReady) {
            Write-Warning "Docker did not start within expected time. Skipping image pull."
        } else {
            Write-Host "Pulling pgvector/pgvector:pg17 image..." -ForegroundColor Cyan
            try {
                docker pull pgvector/pgvector:pg17
                Write-Host "Image pgvector/pgvector:pg17 pulled successfully." -ForegroundColor Green
            } catch {
                Write-Warning "Failed to pull image: $($_.Exception.Message)"
            }
        }
    }
    catch {
        Write-Error "Failed to install Docker Desktop: $($_.Exception.Message)"
    }
}

# Function to install Ngrok
function Install-Ngrok {
    Write-Host "Installing Ngrok..." -ForegroundColor Green

    # Check if Ngrok is already installed
    if (Get-Command ngrok -ErrorAction SilentlyContinue) {
        Write-Host "Ngrok is already installed" -ForegroundColor Yellow
        return
    }

    try {
        # Install Ngrok using winget (if available) or Chocolatey or directly
        if (Get-Command winget -ErrorAction SilentlyContinue) {
            Write-Host "Installing Ngrok via winget..." -ForegroundColor Cyan
            winget install --id=ngrok.ngrok -e --source=winget --accept-source-agreements --accept-package-agreements --force
        }
        elseif (Get-Command choco -ErrorAction SilentlyContinue) {
            Write-Host "Installing Ngrok via Chocolatey..." -ForegroundColor Cyan
            choco install ngrok -y --force
        }
        else {
            # URL for Ngrok zip
            $downloadUrl = "https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-windows-amd64.zip"
            $zipPath = "$env:TEMP\ngrok.zip"
            $extractPath = "$env:USERPROFILE\ngrok"

            Write-Host "Downloading Ngrok..." -ForegroundColor Cyan
            Invoke-WebRequest -Uri $downloadUrl -OutFile $zipPath -ErrorAction Stop

            Write-Host "Extracting Ngrok..." -ForegroundColor Cyan
            if (!(Test-Path $extractPath)) {
                New-Item -ItemType Directory -Path $extractPath -Force
            }
            Expand-Archive -Path $zipPath -DestinationPath $extractPath -Force

            # Add to PATH for the session
            $env:PATH += ";$extractPath"

            Remove-Item $zipPath -Force
        }
        Write-Host "Ngrok installation completed successfully!" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to install Ngrok: $($_.Exception.Message)"
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
Install-DockerDesktop
Install-Ngrok

Write-Host "========================================" -ForegroundColor Magenta
Write-Host "Development Environment Setup Complete!" -ForegroundColor Magenta
Write-Host "Please restart your terminal to ensure all changes take effect." -ForegroundColor Cyan
Write-Host "You may need to restart your computer for all changes to be fully applied." -ForegroundColor Cyan