param(
    [ValidateSet("local", "nuget", "status", "menu")]
    [string]$Mode
)

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$propsPath = Join-Path $PSScriptRoot "..\..\Directory.Build.props"
$propsPath = [System.IO.Path]::GetFullPath($propsPath)

if (-not (Test-Path $propsPath)) {
    Write-Host "[错误] 未找到 Directory.Build.props: $propsPath" -ForegroundColor Red
    exit 1
}

$content = Get-Content $propsPath -Raw

$currentMatch = [regex]::Match($content, '<UseLocalFramework>(true|false)</UseLocalFramework>')
if (-not $currentMatch.Success) {
    Write-Host "[错误] 在 $propsPath 中未找到 <UseLocalFramework> 属性" -ForegroundColor Red
    exit 1
}

$currentValue = $currentMatch.Groups[1].Value
$currentLabel = if ($currentValue -eq "true") { "本地项目引用" } else { "NuGet 包引用" }

if ($Mode -eq "menu") {
    Write-Host ""
    Write-Host "  [1] 切换到本地项目引用"
    Write-Host "  [2] 切换到 NuGet 包引用"
    Write-Host "  [3] 自动切换（取反当前模式）"
    Write-Host "  [4] 查看当前状态"
    Write-Host ""
    $choice = Read-Host "  请选择 (1-4)"

    switch ($choice) {
        "1" { $Mode = "local" }
        "2" { $Mode = "nuget" }
        "3" { $Mode = $null }
        "4" { $Mode = "status" }
        default {
            Write-Host "  无效的选择。" -ForegroundColor Red
            exit 1
        }
    }
}

if (-not $Mode) {
    if ($currentValue -eq "true") { $Mode = "nuget" } else { $Mode = "local" }
}

if ($Mode -eq "status") {
    Write-Host ""
    Write-Host "  当前模式：$currentLabel ($currentValue)" -ForegroundColor Cyan
    Write-Host "  配置文件：$propsPath" -ForegroundColor DarkGray
    Write-Host ""
    exit 0
}

$targetValue = if ($Mode -eq "local") { "true" } else { "false" }
$targetLabel = if ($Mode -eq "local") { "本地项目引用" } else { "NuGet 包引用" }

if ($currentValue -eq $targetValue) {
    Write-Host ""
    Write-Host "  当前已是 $targetLabel 模式，无需切换。" -ForegroundColor Yellow
    Write-Host ""
    exit 0
}

Write-Host ""
Write-Host "  正在切换：$currentLabel -> $targetLabel" -ForegroundColor Cyan
Write-Host ""

$newContent = $content -replace '<UseLocalFramework>(true|false)</UseLocalFramework>', "<UseLocalFramework>$targetValue</UseLocalFramework>"
Set-Content -Path $propsPath -Value $newContent -NoNewline

Write-Host "  [1/2] 已更新 Directory.Build.props" -ForegroundColor Green

$webHostDir = Join-Path $PSScriptRoot "..\..\src\main\XiHan.BasicApp.WebHost"
$webHostDir = [System.IO.Path]::GetFullPath($webHostDir)

Write-Host "  [2/2] 正在还原依赖包 ..." -ForegroundColor DarkGray
Push-Location $webHostDir
dotnet restore --force --verbosity minimal 2>&1 | Out-Null
$restoreExit = $LASTEXITCODE
Pop-Location

if ($restoreExit -eq 0) {
    Write-Host "  [2/2] 依赖还原完成" -ForegroundColor Green
} else {
    Write-Host "  [2/2] 依赖还原失败（退出码 $restoreExit）" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "  切换完成！当前使用：$targetLabel" -ForegroundColor Green
Write-Host ""
