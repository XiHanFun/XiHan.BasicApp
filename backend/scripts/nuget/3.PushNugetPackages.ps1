# 设置 version.props 文件路径
$versionPropsPath = "..\..\props\version.props"
# 读取 version.props 内容，确保以 UTF-8 编码读取
[xml]$xml = [System.Xml.XmlDocument]::new()
$reader = [System.IO.StreamReader]::new($versionPropsPath, [System.Text.Encoding]::UTF8)
$xml.Load($reader)
$reader.Close()
# 获取当前版本信息
$currentVersion = $xml.Project.PropertyGroup.Version
Write-Output "当前版本：$currentVersion"

# NuGet 包路径
$nupkgsPath = "..\..\nupkgs"
# 从环境变量读取 API 密钥
$apiKey = [System.Environment]::GetEnvironmentVariable("NUGET_API_KEY", "User")
# NuGet 源地址
$sourceUrl = "https://api.nuget.org/v3/index.json"

# 获取包路径目录下所有 .nupkg 文件
$nupkgFiles = Get-ChildItem -Path $nupkgsPath -Filter *.nupkg

# 正则表达式匹配版本号，例如
# 1.0.0-alpha.2 将提取成 1、0、0、alpha、2
# 1.0.0-preview 将提取成 1、0、0、preview、0
# 1.0.0 将提取成 1、0、0
# 匹配格式：主版本.次版本.修订版本[-预发布标签.编号]
$regex = '(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z]+)(?:\.(\d+))?)?$'

# 查找并删除与当前版本不匹配的 nupkg 文件
foreach ($file in $nupkgFiles) {
    # 获取文件名
    $fileName = $file.Name
    # 移除 .nupkg 后缀
    $fileName = $fileName.Substring(0, $fileName.Length - 6)

    # 获取文件名中的版本号
    if ($fileName -match $regex) {
        # 提取版本信息
        $currentMajor = [int]$matches[1]
        $currentMinor = [int]$matches[2]
        $currentPatch = [int]$matches[3]
        $currentReleaseTag = if ($matches[4]) { $matches[4].ToLower() } else { "" }
        $currentReleaseNumber = if ($matches[5]) { [int]$matches[5] } else { 0 }

        $fileVersion = "$currentMajor.$currentMinor.$currentPatch"
        if ($currentReleaseTag) {
            $fileVersion = "$fileVersion-$currentReleaseTag"
            if ($currentReleaseNumber -gt 0) {
                $fileVersion = "$fileVersion.$currentReleaseNumber"
            }
        }

        # 比较版本号，删除不匹配的文件
        if ($currentVersion -ne $fileVersion) {
            Write-Output "删除不匹配的文件：$($file.Name)"
            Remove-Item $file.FullName
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
