param(
    [Parameter(Mandatory)]
    [ValidateSet('Debug','Release')]
    [System.String]$Target,
    
    [Parameter(Mandatory)]
    [System.String]$TargetPath,
    
    [Parameter(Mandatory)]
    [System.String]$TargetAssembly,

    [Parameter(Mandatory)]
    [System.String]$ValheimPath,

    [Parameter(Mandatory)]
    [System.String]$ProjectPath,
    
    [System.String]$DeployPath
)

# Make sure Get-Location is the script path
Push-Location -Path (Split-Path -Parent $MyInvocation.MyCommand.Path)

# Test some preliminaries
("$TargetPath",
 "$ValheimPath",
 "$(Get-Location)\..\libraries"
) | % {
    if (!(Test-Path "$_")) {Write-Error -ErrorAction Stop -Message "$_ folder is missing"}
}

# Plugin name without ".dll"
$name = "$TargetAssembly" -Replace('.dll')

# Create the mdb file
$pdb = "$TargetPath\$name.pdb"
if (Test-Path -Path "$pdb") {
    Write-Host "Create mdb file for plugin $name"
    Invoke-Expression "& `"$(Get-Location)\..\libraries\Debug\pdb2mdb.exe`" `"$TargetPath\$TargetAssembly`""
}

# Main Script
Write-Host "Publishing for $Target from $TargetPath"

if ($Target.Equals("Debug")) {
    if ($DeployPath.Equals("")){
      $DeployPath = "$ValheimPath\BepInEx\plugins"
    }
    
    $plug = New-Item -Type Directory -Path "$DeployPath\$name" -Force
    Write-Host "Copy $TargetAssembly to $plug"
    Copy-Item -Path "$TargetPath\$name.dll" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.pdb" -Destination "$plug" -Force
    Copy-Item -Path "$TargetPath\$name.dll.mdb" -Destination "$plug" -Force
	Copy-Item -Path "$ProjectPath\icon_build.png" -Destination "$plug" -Force
	Copy-Item -Path "$ProjectPath\radio" -Destination "$plug" -Force
	Copy-Item -Path "$ProjectPath\Localization.json" -Destination "$plug" -Force
	Copy-Item -Path "$ProjectPath\MonoMod.Backports.dll" -Destination "$plug" -Force
}

if ($Target.Equals("Release")) {
    Write-Host "Packaging for ThunderStore..."
    
    $PackagePath = Join-Path $ProjectPath "Package"
    $pluginsPath = Join-Path $PackagePath "RadioMod"

    # Alten plugins-Ordner löschen und neu anlegen
    if (Test-Path $pluginsPath) { 
        Remove-Item $pluginsPath -Recurse -Force 
    }
    New-Item -ItemType Directory -Path $pluginsPath -Force | Out-Null
	
	# Leeren Audio-Ordner für die Nutzer
    New-Item -ItemType Directory -Path (Join-Path $pluginsPath "Audio") -Force | Out-Null

    # Dateien kopieren
    Copy-Item "$TargetPath\$TargetAssembly" "$pluginsPath\$TargetAssembly" -Force
    Copy-Item "$ProjectPath\Localization.json" $pluginsPath -Force -ErrorAction SilentlyContinue
    Copy-Item "$ProjectPath\radio" $pluginsPath -Force -ErrorAction SilentlyContinue
    Copy-Item "$ProjectPath\icon_build.png" $pluginsPath -Force -ErrorAction SilentlyContinue
	Copy-Item "$ProjectPath\icon.png" $PackagePath -Force -ErrorAction SilentlyContinue
    Copy-Item "$ProjectPath\README.md" $pluginsPath -Force -ErrorAction SilentlyContinue
	Copy-Item "$ProjectPath\MonoMod.Backports.dll" $pluginsPath -Force -ErrorAction SilentlyContinue

    # Zip erstellen
    $zipPath = Join-Path $TargetPath "$name.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    
    Compress-Archive -Path "$PackagePath\*" -DestinationPath $zipPath -Force
    Write-Host "Created package: $zipPath"
}

# Pop Location
Pop-Location