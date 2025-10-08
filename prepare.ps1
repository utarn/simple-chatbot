#Requires -RunAsAdministrator

# Install .NET 8 SDK
Write-Host "Installing .NET 8 SDK..."
winget install Microsoft.DotNet.SDK.8 --accept-source-agreements --accept-package-agreements

# Install Visual Studio Code
Write-Host "Installing Visual Studio Code..."
winget install Microsoft.VisualStudioCode --accept-source-agreements --accept-package-agreements

# Install Git
Write-Host "Installing Git..."
winget install Git.Git --accept-source-agreements --accept-package-agreements

# Install Windows Terminal
Write-Host "Installing Windows Terminal..."
winget install Microsoft.WindowsTerminal --accept-source-agreements --accept-package-agreements

# Install PowerShell 7
Write-Host "Installing PowerShell 7..."
winget install Microsoft.PowerShell --accept-source-agreements --accept-package-agreements

# Install PostgreSQL 17
Write-Host "Installing PostgreSQL 17..."
winget install PostgreSQL.PostgreSQL --accept-source-agreements --accept-package-agreements

Write-Host "All installations completed. You may need to restart your terminal or system for changes to take effect."