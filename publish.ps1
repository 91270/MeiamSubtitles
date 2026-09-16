param (
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$JellyfinVersion,
    
    [Parameter(Mandatory=$false)]
    [string]$EmbyVersion,
    
    [Parameter(Mandatory=$false)]
    [string]$Notes,
    
    [Parameter(Mandatory=$false)]
    [switch]$PublishToGitHub
)

$ErrorActionPreference = "Stop"
$SupportedEmbySdkVersion = "4.8.10"
$SupportedJellyfinSdkVersion = "10.10.7"

Write-Host "=== Start Release Process, Target Version: $Version ===" -ForegroundColor Cyan

# 1. Get current UTC timestamp
$Timestamp = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.0000000Z")
Write-Host "Current package timestamp (UTC): $Timestamp" -ForegroundColor Gray

# Define paths
$ThunderCsproj = "Jellyfin.MeiamSub.Thunder\Jellyfin.MeiamSub.Thunder.csproj"
$ThunderMeta = "Jellyfin.MeiamSub.Thunder\meta.json"
$AssrtCsproj = "Jellyfin.MeiamSub.Assrt\Jellyfin.MeiamSub.Assrt.csproj"
$AssrtMeta = "Jellyfin.MeiamSub.Assrt\meta.json"
$EmbyThunderCsproj = "Emby.MeiamSub.Thunder\Emby.MeiamSub.Thunder.csproj"
$EmbyAssrtCsproj = "Emby.MeiamSub.Assrt\Emby.MeiamSub.Assrt.csproj"

# Update NuGet package version if specified
if ($JellyfinVersion -and $JellyfinVersion -ne $SupportedJellyfinSdkVersion) {
    throw "Jellyfin SDK must remain at $SupportedJellyfinSdkVersion for the verified 10.10.7-12.1 compatibility range. Update the compatibility matrix before changing it."
}

if ($JellyfinVersion) {
    Write-Host "Upgrading Jellyfin.Controller NuGet dependency to $JellyfinVersion..." -ForegroundColor Yellow
    (Get-Content $ThunderCsproj) -replace '<PackageReference Include="Jellyfin.Controller" Version="[^"]+" />', "<PackageReference Include=`"Jellyfin.Controller`" Version=`"$JellyfinVersion`" />" | Set-Content $ThunderCsproj
    (Get-Content $AssrtCsproj) -replace '<PackageReference Include="Jellyfin.Controller" Version="[^"]+" />', "<PackageReference Include=`"Jellyfin.Controller`" Version=`"$JellyfinVersion`" />" | Set-Content $AssrtCsproj
}

if ($EmbyVersion -and $EmbyVersion -ne $SupportedEmbySdkVersion) {
    throw "Emby SDK must remain at $SupportedEmbySdkVersion for the verified 4.8.10.0-4.10.0.40 compatibility range. Update the compatibility matrix before changing it."
}

if ($EmbyVersion) {
    Write-Host "Upgrading MediaBrowser NuGet dependencies to $EmbyVersion..." -ForegroundColor Yellow
    (Get-Content $EmbyThunderCsproj) -replace '<PackageReference Include="MediaBrowser.Common" Version="[^"]+" />', "<PackageReference Include=`"MediaBrowser.Common`" Version=`"$EmbyVersion`" />" `
                                     -replace '<PackageReference Include="MediaBrowser.Server.Core" Version="[^"]+" />', "<PackageReference Include=`"MediaBrowser.Server.Core`" Version=`"$EmbyVersion`" />" | Set-Content $EmbyThunderCsproj
    (Get-Content $EmbyAssrtCsproj) -replace '<PackageReference Include="MediaBrowser.Common" Version="[^"]+" />', "<PackageReference Include=`"MediaBrowser.Common`" Version=`"$EmbyVersion`" />" `
                                     -replace '<PackageReference Include="MediaBrowser.Server.Core" Version="[^"]+" />', "<PackageReference Include=`"MediaBrowser.Server.Core`" Version=`"$EmbyVersion`" />" | Set-Content $EmbyAssrtCsproj
}

Write-Host "Updating version and timestamp in project files..." -ForegroundColor Yellow

# Update Jellyfin Thunder
(Get-Content $ThunderCsproj) -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>" `
                             -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>" | Set-Content $ThunderCsproj

(Get-Content $ThunderMeta) -replace '"version":\s*"[\d\.]+"', "`"version`": `"$Version`"" `
                           -replace '"timestamp":\s*"[^"]+"', "`"timestamp`": `"$Timestamp`"" | Set-Content $ThunderMeta

# Update Jellyfin Assrt
(Get-Content $AssrtCsproj) -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>" `
                           -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>" `
                           -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$Version</FileVersion>" | Set-Content $AssrtCsproj

(Get-Content $AssrtMeta) -replace '"version":\s*"[\d\.]+"', "`"version`": `"$Version`"" `
                         -replace '"timestamp":\s*"[^"]+"', "`"timestamp`": `"$Timestamp`"" | Set-Content $AssrtMeta

# Update Emby Thunder
(Get-Content $EmbyThunderCsproj) -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>" `
                                 -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>" `
                                 -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$Version</FileVersion>" | Set-Content $EmbyThunderCsproj

# Update Emby Assrt
(Get-Content $EmbyAssrtCsproj) -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>" `
                               -replace '<AssemblyVersion>[\d\.]+</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>" `
                               -replace '<FileVersion>[\d\.]+</FileVersion>', "<FileVersion>$Version</FileVersion>" | Set-Content $EmbyAssrtCsproj

Write-Host "Versions and timestamps updated successfully!" -ForegroundColor Green

# 2. Compile solution
Write-Host "Compiling solution in Release mode..." -ForegroundColor Yellow
dotnet build MeiamSubtitles.sln -c Release

# 3. Create zip archives
Write-Host "Packaging zip archives..." -ForegroundColor Yellow
$TempDir = "Release\temp_pack"
$EmbyPackDir = "$TempDir\Emby"
$JellyfinThunderPackDir = "$TempDir\Jellyfin\Jellyfin.MeiamSub.Thunder_$Version"
$JellyfinAssrtPackDir = "$TempDir\Jellyfin\Jellyfin.MeiamSub.Assrt_$Version"

# Clean temp directories
if (Test-Path $TempDir) { Remove-Item -Recurse -Force $TempDir }
New-Item -ItemType Directory -Force -Path $EmbyPackDir | Out-Null
New-Item -ItemType Directory -Force -Path $JellyfinThunderPackDir | Out-Null
New-Item -ItemType Directory -Force -Path $JellyfinAssrtPackDir | Out-Null

# Copy Emby DLLs
Copy-Item Release\Emby.MeiamSub.Thunder.dll $EmbyPackDir\
Copy-Item Release\Emby.MeiamSub.Assrt.dll $EmbyPackDir\

# Copy Jellyfin Thunder artifacts
Copy-Item Release\Jellyfin.MeiamSub.Thunder.dll $JellyfinThunderPackDir\
Copy-Item Jellyfin.MeiamSub.Thunder\meta.json $JellyfinThunderPackDir\
Copy-Item Jellyfin.MeiamSub.Thunder\thumb.png $JellyfinThunderPackDir\

# Copy Jellyfin Assrt artifacts
Copy-Item Release\Jellyfin.MeiamSub.Assrt.dll $JellyfinAssrtPackDir\
Copy-Item Release\dependencies\Jellyfin\* $JellyfinAssrtPackDir\
Copy-Item Jellyfin.MeiamSub.Assrt\meta.json $JellyfinAssrtPackDir\
Copy-Item Jellyfin.MeiamSub.Assrt\thumb.png $JellyfinAssrtPackDir\

# Archive target paths
$EmbyZip = "Release\Emby_v$Version.zip"
$JellyfinZip = "Release\Jellyfin_v$Version.zip"

if (Test-Path $EmbyZip) { Remove-Item -Force $EmbyZip }
if (Test-Path $JellyfinZip) { Remove-Item -Force $JellyfinZip }

# Compress
Compress-Archive -Path "$EmbyPackDir\*" -DestinationPath $EmbyZip -Force
Compress-Archive -Path "$TempDir\Jellyfin\*" -DestinationPath $JellyfinZip -Force

# Cleanup temp
Remove-Item -Recurse -Force $TempDir

Write-Host "=== Packaging Finished successfully! ===" -ForegroundColor Green
Write-Host "Emby Package: $EmbyZip" -ForegroundColor Gray
Write-Host "Jellyfin Package: $JellyfinZip" -ForegroundColor Gray

# 4. Publish to GitHub Release if specified
if ($PublishToGitHub) {
    Write-Host "=== Start GitHub Release process ===" -ForegroundColor Cyan
    
    Write-Host "Staging and committing release changes..." -ForegroundColor Yellow
    git add .
    git commit -m "release: v$Version"
    
    Write-Host "Pushing changes to remote repository..." -ForegroundColor Yellow
    git push origin master
    
    Write-Host "Creating GitHub Release v$Version and uploading assets..." -ForegroundColor Yellow
    if ($Notes) {
        $AutoNotes = gh release generate-notes --tag "v$Version"
        $NotesContent = "## Release Notes`n`n$Notes`n`n" + $AutoNotes
        $NotesFile = "Release\temp_notes.md"
        Set-Content -Path $NotesFile -Value $NotesContent
        
        gh release create "v$Version" $EmbyZip $JellyfinZip --title "v$Version" --notes-file $NotesFile
        if (Test-Path $NotesFile) { Remove-Item -Force $NotesFile }
    } else {
        gh release create "v$Version" $EmbyZip $JellyfinZip --title "v$Version" --generate-notes
    }
    
    Write-Host "=== GitHub Release finished successfully! ===" -ForegroundColor Green
}
