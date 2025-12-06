param (
    [switch]$run,
    [switch]$test
)

$ErrorActionPreference = "Stop"

$modName = "YAMP"
$sourceDir = "C:\Users\Andrei\Documents\Repos\YAMP"
$destDir = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\$modName"

try {
    Write-Host "Building $modName..."
    Set-Location "$sourceDir\Source"
    dotnet build -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
    }

    Write-Host "Build" -NoNewline
    Write-Host " [OK]" -ForegroundColor Green

    if ($test) {
        Set-Location "$sourceDir"
        dotnet test --logger 'console;verbosity=detailed'
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Tests failed!"
        }

        Write-Host "Tests" -NoNewline
        Write-Host " [OK]" -ForegroundColor Green
    }

    Write-Host "Deploying to $destDir..."
    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir | Out-Null
    }

    # Copy folders
    $folders = @("About", "Defs", "Textures", "Assemblies")
    foreach ($folder in $folders) {
        $src = "$sourceDir\Mod\$folder"
        try {
            if (Test-Path $src) {
                Write-Host "Copying $folder..." -NoNewline
                Copy-Item -Path $src -Destination $destDir -Recurse -Force
                Write-Host " [OK]" -ForegroundColor Green
            }
            else {
                Write-Host " [MISSING]" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "[ERROR]" -ForegroundColor Red
            throw $_
        }
    }

    Write-Host "Deployment complete!"

    if ($run) {
        Write-Host "Killing running RimWorld instances..."
        Stop-Process -Name "RimWorldWin64" -ErrorAction SilentlyContinue

        $devDataPath = "C:\RimworldDevData\Saves"

        Write-Host "Launching RimWorld..."
        Start-Process "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64.exe" -ArgumentList "-savedatafolder=`"$devDataPath`" -quicktest"
    }
}
catch {
    Write-Error $_.Exception.Message
    Write-Error "Deployment failed!"
}

finally {
    Set-Location "$sourceDir"
}
