# 设置 version.props 文件路径
$versionPropsPath = "..\..\props\version.props"
$projectPath = "..\..\XiHan.App.sln"
# 读取 version.props 内容，确保以 UTF-8 编码读取
[xml]$xml = [System.Xml.XmlDocument]::new()
$reader = [System.IO.StreamReader]::new($versionPropsPath, [System.Text.Encoding]::UTF8)
$xml.Load($reader)
$reader.Close()
# 获取当前版本信息
$currentVersion = $xml.Project.PropertyGroup.Version
# 去除版本号中的任何后缀（包括 -preview、-preview3、-preview15 等）
if ($currentVersion -match '^\d+\.\d+\.\d+') {
    $currentVersion = $matches[0]
}
$currentMajor = [int]$currentVersion.Split('.')[0]
$currentMinor = [int]$currentVersion.Split('.')[1]
$currentPatch = [int]$currentVersion.Split('.')[2]
Write-Output "当前基础版本：$currentVersion"

# 提示用户选择升级类型
$upgradeType = Read-Host "请选择升级类型 (1: 主版本, 2: 大版本, 3: 小版本)"
# 根据用户选择的升级类型更新版本号
switch ($upgradeType) {
    1 {
        $currentMajor = $currentMajor + 1
        $currentMinor = 0
        $currentPatch = 0
    }
    2 {
        $currentMinor = $currentMinor + 1
        $currentPatch = 0
    }
    3 {
        $currentPatch = $currentPatch + 1
    }
    default {
        Write-Output "无效的选择，退出程序"
        exit
    }
}
$newVersion = "$currentMajor.$currentMinor.$currentPatch"
Write-Output "升级后的版本为：$newVersion"

# 更新 XML 节点中的版本信息
$xml.Project.PropertyGroup.AssemblyVersion = $newVersion
$xml.Project.PropertyGroup.FileVersion = $newVersion
$xml.Project.PropertyGroup.Version = $newVersion

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
$confirm = Read-Host "是否确认构建项目？(Y/N)"
if ($confirm -ne "Y") {
    Write-Output "取消构建项目"
    exit
}
Write-Output "正在构建项目……"
dotnet build $projectPath -c Release
