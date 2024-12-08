# 设置 PowerShell 脚本编码为 UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 设置原文件路径和目标文件夹路径
$SRC_FOLDER = Get-Location
$CLASS_DEST_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\CSharp\Code\2052\Class"
$CLASS_CORE_DEST_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\AspNetCore\Code\1033\Class"
$INTERFACE_DEST_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\CSharp\Code\2052\Interface"
$INTERFACE_CORE_DEST_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\AspNetCore\Code\1033\Interface"
$CONTROLLER_DEST_PATH = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\AspNetCore\Web\ASP.NET\1033\WebApiEmptyController"

# 显示当前路径
Write-Host "当前路径：$SRC_FOLDER"
Write-Host ""

# 定义文件复制函数
function Copy-File {
    param (
        [string]$FileName,
        [string]$DestPath1,
        [string]$DestPath2 = $null
    )

    $SourceFile = Join-Path $SRC_FOLDER $FileName

    if (Test-Path $SourceFile) {
        Write-Host "正在复制 '$FileName' ..."

        try {
            Copy-Item -Path $SourceFile -Destination $DestPath1 -Force
            Write-Host "已复制到 '$DestPath1'"
        }
        catch {
            Write-Host "复制到 '$DestPath1' 失败！错误：$($_.Exception.Message)"
        }

        if ($DestPath2) {
            try {
                Copy-Item -Path $SourceFile -Destination $DestPath2 -Force
                Write-Host "已复制到 '$DestPath2'"
            }
            catch {
                Write-Host "复制到 '$DestPath2' 失败！错误：$($_.Exception.Message)"
            }
        }

        Write-Host ""
    }
    else {
        Write-Host "'$FileName' 文件不存在，请检查后重试！"
        Write-Host ""
    }
}

# 调用文件复制函数
Copy-File -FileName "Class.cs" -DestPath1 $CLASS_DEST_PATH -DestPath2 $CLASS_CORE_DEST_PATH
Copy-File -FileName "Interface.cs" -DestPath1 $INTERFACE_DEST_PATH -DestPath2 $INTERFACE_CORE_DEST_PATH
Copy-File -FileName "Controller.cs" -DestPath1 $CONTROLLER_DEST_PATH
