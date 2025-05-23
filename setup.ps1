# Get the directory where the script is located (project root)
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host "--- Running setup for Windows PowerShell ---"

# 1. pip install -r requirements.txt
$requirementsPath = Join-Path $PSScriptRoot "requirements.txt"
if (Test-Path $requirementsPath) {
    Write-Host "Installing Python requirements from $requirementsPath..."
    # Execute pip in a way that its output is shown and error handled
    try {
        & pip install -r $requirementsPath | Out-Host
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error installing requirements. Please check your Python installation." -ForegroundColor Red
            return # Exit the script if installation fails
        }
    } catch {
        Write-Host "An error occurred during pip installation: $($_.Exception.Message)" -ForegroundColor Red
        return
    }
} else {
    Write-Host "requirements.txt not found at $requirementsPath. Skipping requirements installation." -ForegroundColor Yellow
}

# 2. Change directory to 'agent' for pip install
$agentDir = Join-Path $PSScriptRoot "agent"
if (Test-Path $agentDir -PathType Container) {
    Write-Host "Changing directory to $agentDir"
    Set-Location -Path $agentDir
} else {
    Write-Host "Agent directory not found at $agentDir. Cannot proceed with agent setup." -ForegroundColor Red
    return # Exit if agent directory doesn't exist
}

# 3. pip install -e . (in agent directory)
Write-Host "Installing agent package..."
try {
    & pip install -e . | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error installing agent package. Please check your Python installation or agent setup." -ForegroundColor Red
        return
    }
} catch {
    Write-Host "An error occurred during pip installation for agent: $($_.Exception.Message)" -ForegroundColor Red
    return
}

# 4. Change directory back to project root
Write-Host "Changing directory back to $PSScriptRoot"
Set-Location -Path $PSScriptRoot


# 5. Set PYTHONPATH environment variables for the CURRENT session
Write-Host "Setting PYTHONPATH for the CURRENT PowerShell session..."
$pathsToAdd = @(
    (Join-Path $PSScriptRoot "env"),
    $PSScriptRoot,
    (Join-Path $PSScriptRoot "agent" "stardojo"),
    (Join-Path $PSScriptRoot "agent")
)

# Use a set for efficient checking of existing paths
$existingPythonPaths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
if (-not [string]::IsNullOrEmpty($env:PYTHONPATH)) {
    $env:PYTHONPATH.Split(';') | ForEach-Object {
        if (-not [string]::IsNullOrWhiteSpace($_)) {
            $existingPythonPaths.Add($_.Trim().Replace('/', '\')) # Normalize to backslashes
        }
    }
}

foreach ($path in $pathsToAdd) {
    $normalizedPath = $path.Replace('/', '\') # Normalize to backslashes
    if (-not $existingPythonPaths.Contains($normalizedPath)) {
        $env:PYTHONPATH += ";$normalizedPath"
        Write-Host "Added to PYTHONPATH: $normalizedPath"
    } else {
        Write-Host "PYTHONPATH already contains: $normalizedPath" -ForegroundColor Yellow
    }
}

Write-Host "Final PYTHONPATH: $($env:PYTHONPATH)"
Write-Host "--- Setup complete. You can now run other commands in this terminal. ---"