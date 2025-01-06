# 设置 version.props 文件路径
$versionPropsPath = "..\..\props\version.props"
$projectPath = "..\..\XiHan.BasicApp.sln"
# 读取 version.props 内容，确保以 UTF-8 编码读取
[xml]$xml = [System.Xml.XmlDocument]::new()
$reader = [System.IO.StreamReader]::new($versionPropsPath, [System.Text.Encoding]::UTF8)
$xml.Load($reader)
$reader.Close()
# 获取当前版本信息
$currentVersion = $xml.Project.PropertyGroup.Version
Write-Output "当前版本：$currentVersion"

# 正则表达式匹配版本号，例如
# 1.0.0-alpha.2 将提取成 1、0、0、alpha、2
# 1.0.0-preview 将提取成 1、0、0、preview、0
# 1.0.0 将提取成 1、0、0
# 匹配格式：主版本.次版本.修订版本[-预发布标签.编号]
$regex = '^(\d+)\.(\d+)\.(\d+)(?:-([a-zA-Z]+)(?:\.(\d+))?)?$'

if ($currentVersion -match $regex) {
    # 提取版本信息
    $currentMajor = [int]$matches[1]
    $currentMinor = [int]$matches[2]
    $currentPatch = [int]$matches[3]
    $currentReleaseTag = if ($matches[4]) { $matches[4].ToLower() } else { "" }
    $currentReleaseNumber = if ($matches[5]) { [int]$matches[5] } else { 0 }
} else {
    Write-Error "版本格式无效，请提供有效的版本字符串。"
}
Write-Output "主版本: $currentMajor，次版本: $currentMinor，修订版本: $currentPatch，发布标签: $currentReleaseTag，发布编号: $currentReleaseNumber"

# 提示用户选择升级类型
Write-Output "请选择升级类型："
Write-Output "0: 不升级"
Write-Output "1: 主版本"
Write-Output "2: 大版本"
Write-Output "3: 小版本"
$upgradeType = Read-Host ">>> 请选择升级类型 (0-3)"
switch ($upgradeType) {
    0 {
        Write-Output "不升级"
    }
    1 {
        $currentMajor = $currentMajor + 1
        $currentMinor = 0
        $currentPatch = 0
        $currentReleaseTag = ""
        $currentReleaseNumber = 0
    }
    2 {
        $currentMinor = $currentMinor + 1
        $currentPatch = 0
        $currentReleaseTag = ""
        $currentReleaseNumber = 0
    }
    3 {
        $currentPatch = $currentPatch + 1
        $currentReleaseTag = ""
        $currentReleaseNumber = 0
    }
    default {
        Write-Output "无效的选择，退出程序"
        exit
    }
}
$newVersion = "$currentMajor.$currentMinor.$currentPatch"
Write-Output "升级基础版本号为：$newVersion"

# 提示用户选择发布类型
# NuGet 遵循 SemVer 2.0 标准，版本号会根据主版本、次版本、修订号以及后缀进行排序。1.0.0-alpha < 1.0.0-beta < 1.0.0-preview < 1.0.0-rc < 1.0.0
Write-Output "请选择发布类型："
Write-Output "0: 不升级"
Write-Output "1: 开发版 Alpha，表示包处于非常早期的开发阶段，可能非常不稳定，如 1.0.0-alpha 或 1.0.0-alpha.1"
Write-Output "2: 测试版 Beta，表示包处于测试阶段，可能包含一些不稳定的功能，如 1.0.0-beta 或 1.0.0-beta.1"
Write-Output "3: 预览版 Preview，表示包处于测试阶段，可能包含一些不稳定的功能，如 1.0.0-preview 或 1.0.0-preview.1"
Write-Output "4: 候选版 Rc，表示包处于测试阶段，可能包含一些不稳定的功能，如 1.0.0-rc 或 1.0.0-rc.1"
Write-Output "5: 稳定版 Release，表示包已经稳定，不包含任何不稳定的功能，如 1.0.0"
$releaseType = Read-Host ">>> 请选择发布类型 (0-5)"
switch ($releaseType) {
    0 {
        Write-Output "不升级"
    }
    1 {
        # 如果当前发布标签已经为 Alpha，则升级发布编号
        if ($currentReleaseTag -eq "alpha") {
            $currentReleaseNumber = $currentReleaseNumber + 1
        }
        else {
            $currentReleaseTag = "alpha"
            $currentReleaseNumber = 0
        }
    }
    2 {
        # 如果当前发布标签已经为 Beta，则升级发布编号
        if ($currentReleaseTag -eq "beta") {
            $currentReleaseNumber = $currentReleaseNumber + 1
        }
        else {
            $currentReleaseTag = "beta"
            $currentReleaseNumber = 0
        }
    }
    3 {
        # 如果当前发布标签已经为 Preview，则升级发布编号
        if ($currentReleaseTag -eq "preview") {
            $currentReleaseNumber = $currentReleaseNumber + 1
        }
        else {
            $currentReleaseTag = "preview"
            $currentReleaseNumber = 0
        }
    }
    4 {
        # 如果当前发布标签已经为 Rc，则升级发布编号
        if ($currentReleaseTag -eq "rc") {
            $currentReleaseNumber = $currentReleaseNumber + 1
        }
        else {
            $currentReleaseTag = "rc"
            $currentReleaseNumber = 0
        }
    }
    5 {
        $currentReleaseTag = ""
        $currentReleaseNumber = 0
    }
    default {
        Write-Output "无效的选择，退出程序"
        exit
    }
}
if ($currentReleaseTag) {
    $newReleaseVersion = "$newVersion-$currentReleaseTag"

    # 如果当前发布编号大于 0，添加发布编号
    if ($currentReleaseNumber -gt 0) {
        $newReleaseVersion = "$newReleaseVersion.$currentReleaseNumber"
    }
}
else {
    $newReleaseVersion = "$newVersion"
}
Write-Output "升级发布版本号为：$newReleaseVersion"

# 更新 XML 节点中的版本信息，程序集版本号、文件版本号不能包含发布标签，版本号可以包含发布标签
$xml.Project.PropertyGroup.AssemblyVersion = $newVersion
$xml.Project.PropertyGroup.FileVersion = $newVersion
$xml.Project.PropertyGroup.Version = $newReleaseVersion

# 使用 XmlWriter 进行格式化保存
$settings = New-Object System.Xml.XmlWriterSettings
$settings.Indent = $true           # 开启缩进
$settings.IndentChars = "  "     # 定义缩进字符
$settings.NewLineOnAttributes = $true  # 在属性后插入换行符
$settings.OmitXmlDeclaration = $true  # 忽略 XML 声明
$settings.Encoding = [System.Text.Encoding]::UTF8
$settings.CloseOutput = $true

# 使用 StreamWriter 和 XmlWriter 将内容保存为格式化后的 XML
$writer = [System.Xml.XmlWriter]::Create($versionPropsPath, $settings)
$xml.Save($writer)
$writer.Close()

# 执行项目构建
$confirm = Read-Host ">>> 是否确认构建项目？(Y/N)"
if ($confirm -ne "Y") {
    Write-Output "取消构建项目"
    exit
}
Write-Output "正在构建项目……"
dotnet build $projectPath -c Release
