# 设置 version.props 文件路径
$versionPropsPath = "..\..\props\version.props"
# 读取 version.props 内容，确保以 UTF-8 编码读取
[xml]$xml = [System.Xml.XmlDocument]::new()
$reader = [System.IO.StreamReader]::new($versionPropsPath, [System.Text.Encoding]::UTF8)
$xml.Load($reader)
$reader.Close()
# 获取当前版本信息
$currentVersion = $xml.Project.PropertyGroup.Version

# NuGet 包路径
$nupkgsPath = "..\..\nupkgs"
# 从环境变量读取 API 密钥
$apiKey = [System.Environment]::GetEnvironmentVariable("NUGET_API_KEY", "User")
# NuGet 源地址
$sourceUrl = "https://api.nuget.org/v3/index.json"

# 获取包路径目录下所有 .nupkg 文件
$nupkgFiles = Get-ChildItem -Path $nupkgsPath -Filter *.nupkg

# 获取当前版本信息
$currentVersion = [Version]$currentVersion

# 查找并删除与当前版本不匹配的 nupkg 文件
foreach ($file in $nupkgFiles) {
    if ($file.Name -match "^(.*?)\.(\d+)\.(\d+)\.(\d+)\.nupkg$") {
        $version = [Version]::Parse("$($matches[2]).$($matches[3]).$($matches[4])")
        
        # 如果版本与当前版本不匹配，删除该文件
        if ($version -ne $currentVersion) {
            Write-Output "删除与当前版本不匹配的 nupkg 文件：$($file.Name)"
            Remove-Item -Path $file.FullName -Force
        }
    }
}

# 是否确认推送
$confirm = Read-Host "是否确认推送最新的 nupkg 文件？(Y/N)"
if ($confirm -ne "Y") {
    Write-Output "取消推送"
    exit
}

# 获取剩余文件（应只包含当前版本的文件）
$nupkgFiles = Get-ChildItem -Path $nupkgsPath -Filter *.nupkg

# 推送当前版本的 nupkg 文件
foreach ($file in $nupkgFiles) {
    Write-Output "正在推送文件：$($file.Name)"
    dotnet nuget push $file.FullName --api-key $apiKey --source $sourceUrl --skip-duplicate
    if ($LASTEXITCODE -eq 0) {
        Write-Output "推送成功：$($file.Name)"
    }
    else {
        Write-Output "推送失败：$($file.Name)"
    }
}
