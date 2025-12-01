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

    Write-Host "Deploying to $destDir..."

    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir | Out-Null
    }

    # Copy folders
    $folders = @("About", "Defs", "Textures", "Assemblies")
    foreach ($folder in $folders) {
        $src = "$sourceDir\$folder"
        $dst = "$destDir\$folder"
    
        try
        {
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
        Write-Error "[ERROR]" -ForegroundColor Red
        THROW $_.Exception.Message
    }
    }

    Write-Host "Deployment complete!"
}
catch {
    Write-Error $_.Exception.Message
    Write-Error "Deployment failed!"
}

finally {
    Set-Location "$sourceDir"

}
