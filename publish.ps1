param (
    [Parameter(Mandatory=$true)]
    [string]$Version,

    [Parameter(Mandatory=$false)]
    [ValidateSet("Stable", "Preview")]
    [string]$Channel = "Stable",
    
    [Parameter(Mandatory=$false)]
    [string]$JellyfinVersion,
    
    [Parameter(Mandatory=$false)]
    [string]$EmbyVersion,
    
    [Parameter(Mandatory=$false)]
    [string]$Notes,
    
    [Parameter(Mandatory=$false)]
    [switch]$PublishToGitHub,

    [Parameter(Mandatory=$false)]
    [switch]$ValidateOnly
)

$ErrorActionPreference = "Stop"
$SupportedEmbySdkVersion = "4.8.10"
$SupportedJellyfinSdkVersion = "10.10.7"
$RepositoryRoot = (Resolve-Path $PSScriptRoot).Path.Replace('\', '/')
$GitArguments = @("-c", "safe.directory=$RepositoryRoot")
$PreviewTag = "preview"

# Define paths
$ThunderCsproj = "Jellyfin.MeiamSub.Thunder\Jellyfin.MeiamSub.Thunder.csproj"
$ThunderMeta = "Jellyfin.MeiamSub.Thunder\meta.json"
$AssrtCsproj = "Jellyfin.MeiamSub.Assrt\Jellyfin.MeiamSub.Assrt.csproj"
$AssrtMeta = "Jellyfin.MeiamSub.Assrt\meta.json"
$EmbyThunderCsproj = "Emby.MeiamSub.Thunder\Emby.MeiamSub.Thunder.csproj"
$EmbyAssrtCsproj = "Emby.MeiamSub.Assrt\Emby.MeiamSub.Assrt.csproj"

if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "版本号必须使用四段数字格式，例如 1.0.18.0 或 1.0.17.1。"
}

$VersionParts = $Version.Split('.') | ForEach-Object { [int]$_ }
$Revision = $VersionParts[3]

if ($Channel -eq "Stable" -and $Revision -ne 0) {
    throw "正式版版本号末位必须为 0，例如 1.0.18.0。"
}

if ($Channel -eq "Preview" -and $Revision -eq 0) {
    throw "测试版版本号末位必须大于 0，例如 1.0.17.1。"
}

if ($Channel -eq "Preview" -and $PublishToGitHub -and [string]::IsNullOrWhiteSpace($Notes)) {
    throw "发布滚动测试版时必须通过 -Notes 说明本轮测试重点。"
}

$CurrentVersionMatch = [regex]::Match((Get-Content -LiteralPath $ThunderCsproj -Raw), '<Version>([\d\.]+)</Version>')
if (-not $CurrentVersionMatch.Success) {
    throw "无法读取当前插件版本。"
}
$CurrentVersion = [version]$CurrentVersionMatch.Groups[1].Value
if ([version]$Version -le $CurrentVersion) {
    throw "目标版本 $Version 必须高于当前版本 $CurrentVersion。"
}

$WorkingTreeChanges = @(git @GitArguments status --porcelain)
if ($LASTEXITCODE -ne 0) {
    throw "无法检查 Git 工作树状态。"
}

if ($WorkingTreeChanges.Count -gt 0) {
    throw "发布前工作树必须保持干净，请先提交或处理现有变更。"
}

if ($Channel -eq "Stable") {
    $ExistingLocalTag = git @GitArguments tag --list "v$Version"
    if ($LASTEXITCODE -ne 0) {
        throw "无法检查本地 Git 标签。"
    }
    if ($ExistingLocalTag) {
        throw "本地标签 v$Version 已存在，禁止覆盖已经使用的正式版本。"
    }

    if ($PublishToGitHub) {
        git @GitArguments ls-remote --exit-code --tags origin "refs/tags/v$Version" | Out-Null
        if ($LASTEXITCODE -eq 0) {
            throw "远端标签 v$Version 已存在，禁止覆盖已经公开的正式版本。"
        }
        if ($LASTEXITCODE -ne 2) {
            throw "无法检查远端标签，请检查网络或 Git 认证。"
        }
    }
}

$ChannelLabel = if ($Channel -eq "Preview") { "测试版发布" } else { "正式发布" }
Write-Host "=== 开始${ChannelLabel}流程，目标版本：$Version ===" -ForegroundColor Cyan

if ($ValidateOnly) {
    Write-Host "发布参数、工作树和标签检查通过。" -ForegroundColor Green
    return
}

# 1. Get current UTC timestamp
$Timestamp = [DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.0000000Z")
Write-Host "Current package timestamp (UTC): $Timestamp" -ForegroundColor Gray

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
if ($LASTEXITCODE -ne 0) {
    throw "解决方案编译失败。"
}

Write-Host "Running automated tests..." -ForegroundColor Yellow
dotnet test tests\MeiamSubtitles.Tests\MeiamSubtitles.Tests.csproj -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "自动测试失败。"
}

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

# Archive target paths. Preview assets use stable names because the single
# rolling preview release replaces them on every test iteration.
if ($Channel -eq "Preview") {
    $EmbyZip = "Release\Emby_preview.zip"
    $JellyfinZip = "Release\Jellyfin_preview.zip"
} else {
    $EmbyZip = "Release\Emby_v$Version.zip"
    $JellyfinZip = "Release\Jellyfin_v$Version.zip"
}

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
    $ReleaseMetadataFiles = @(
        $ThunderCsproj,
        $ThunderMeta,
        $AssrtCsproj,
        $AssrtMeta,
        $EmbyThunderCsproj,
        $EmbyAssrtCsproj
    )
    git @GitArguments add -- $ReleaseMetadataFiles
    if ($LASTEXITCODE -ne 0) {
        throw "暂存发布版本文件失败。"
    }

    git @GitArguments diff --cached --check
    if ($LASTEXITCODE -ne 0) {
        throw "暂存内容存在空白错误。"
    }

    $CommitMessage = if ($Channel -eq "Preview") {
        "发布: 更新滚动测试版至 v$Version"
    } else {
        "发布: v$Version"
    }
    git @GitArguments commit -m $CommitMessage
    if ($LASTEXITCODE -ne 0) {
        throw "创建发布提交失败。"
    }
    
    Write-Host "Pushing changes to remote repository..." -ForegroundColor Yellow
    git @GitArguments push origin master
    if ($LASTEXITCODE -ne 0) {
        throw "推送发布提交失败。"
    }
    
    if ($Channel -eq "Preview") {
        Write-Host "Updating rolling GitHub test release..." -ForegroundColor Yellow
        $CommitSha = git @GitArguments rev-parse HEAD
        if ($LASTEXITCODE -ne 0) {
            throw "无法读取预发布提交哈希。"
        }

        $EmbyHash = (Get-FileHash -LiteralPath $EmbyZip -Algorithm SHA256).Hash
        $JellyfinHash = (Get-FileHash -LiteralPath $JellyfinZip -Algorithm SHA256).Hash
        $PreviewNotesFile = "Release\preview-notes.md"
        $PreviewNotes = @"
## 当前测试版本

- 插件版本：$Version
- 源码提交：$CommitSha
- Emby 包 SHA-256：$EmbyHash
- Jellyfin 包 SHA-256：$JellyfinHash

## 本轮测试重点

$Notes

## 安装说明

停止 Emby 或 Jellyfin，移除旧版 Thunder DLL 或插件目录，再安装本页最新测试包并重启。反馈问题时请注明插件版本和宿主版本。
"@
        Set-Content -LiteralPath $PreviewNotesFile -Value $PreviewNotes -Encoding utf8

        try {
            # The user requested one rolling preview. Only this dedicated tag
            # may be force-moved; branches and formal release tags remain immutable.
            git @GitArguments tag --force $PreviewTag $CommitSha
            if ($LASTEXITCODE -ne 0) {
                throw "更新本地 preview 标签失败。"
            }
            git @GitArguments push origin "refs/tags/$PreviewTag" --force
            if ($LASTEXITCODE -ne 0) {
                throw "更新远端 preview 标签失败。"
            }

            & gh release view $PreviewTag *> $null
            $PreviewReleaseExists = $LASTEXITCODE -eq 0

            if ($PreviewReleaseExists) {
                & gh release upload $PreviewTag $EmbyZip $JellyfinZip --clobber
                if ($LASTEXITCODE -ne 0) {
                    throw "覆盖滚动测试附件失败；请检查仓库是否启用了 Release 不可变保护。"
                }
                & gh release edit $PreviewTag --title "测试版（滚动更新）" --notes-file $PreviewNotesFile --prerelease --draft=false --latest=false
            } else {
                & gh release create $PreviewTag $EmbyZip $JellyfinZip --title "测试版（滚动更新）" --verify-tag --notes-file $PreviewNotesFile --prerelease --latest=false
            }

            if ($LASTEXITCODE -ne 0) {
                throw "创建或更新滚动测试 Release 失败。"
            }
        } finally {
            if (Test-Path -LiteralPath $PreviewNotesFile) {
                Remove-Item -LiteralPath $PreviewNotesFile -Force
            }
        }

        Write-Host "滚动测试版已更新：不要修改 manifest-stable.json。" -ForegroundColor Yellow
    } else {
        Write-Host "Creating stable GitHub Release v$Version and uploading assets..." -ForegroundColor Yellow
        $ReleaseArguments = @(
            "release", "create", "v$Version",
            $EmbyZip,
            $JellyfinZip,
            "--title", "v$Version",
            "--target", "master",
            "--latest"
        )
        if ($Notes) {
            $ReleaseArguments += @("--notes", $Notes, "--generate-notes")
        } else {
            $ReleaseArguments += "--generate-notes"
        }
        & gh @ReleaseArguments
        if ($LASTEXITCODE -ne 0) {
            throw "创建正式 GitHub Release 失败。"
        }

        & gh release view $PreviewTag *> $null
        if ($LASTEXITCODE -eq 0) {
            & gh release edit $PreviewTag --draft
            if ($LASTEXITCODE -ne 0) {
                throw "正式版已创建，但隐藏滚动测试 Release 失败。"
            }
        }

        Write-Host "正式版 GitHub Release 已创建，请继续更新 MeiamSubtitles.Release 的稳定清单。" -ForegroundColor Yellow
    }
    
    Write-Host "=== GitHub Release finished successfully! ===" -ForegroundColor Green
}
