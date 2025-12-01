$ErrorActionPreference = "Stop"

$modName = "YAMP"
$sourceDir = "C:\Users\Andrei\Documents\Repos\YAMP"
$destDir = "C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\$modName"

try {
    Write-Host "Building $modName..."
    Set-Location "$sourceDir\Source\YAMP"
    dotnet build -c Release

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed!"
    }

    Write-Host "Deploying to $destDir..."

    if (-not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir | Out-Null
    }

    # Copy folders
    $folders = @("About", "Defs", "Textures")
    foreach ($folder in $folders) {
        $src = "$sourceDir\$folder"
        $dst = "$destDir\$folder"
    
        if (Test-Path $src) {
            Write-Host "Copying $folder..."
            Copy-Item -Path $src -Destination $destDir -Recurse -Force
        }
    }

    # Copy Assembly
    $assemblyDir = "$destDir\Assemblies"
    if (-not (Test-Path $assemblyDir)) {
        New-Item -ItemType Directory -Path $assemblyDir | Out-Null
    }

    $dllSrc = "$sourceDir\Source\YAMP\bin\Release\net472\YAMP.dll"
    if (Test-Path $dllSrc) {
        Write-Host "Copying Assembly..."
        Copy-Item -Path $dllSrc -Destination "$assemblyDir\YAMP.dll" -Force
    }
    else {
        Write-Error "DLL not found at $dllSrc"
    }
    Write-Host "Deployment complete!"
}
catch {
    Write-Error "Deployment failed!"
}

finally {
    Set-Location "$sourceDir"

}
