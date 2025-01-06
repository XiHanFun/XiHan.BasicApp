# 获取当前脚本所在目录
$ScriptDirectory = $PSScriptRoot

# 构建相对路径的目标目录
$RootPath = Join-Path -Path $ScriptDirectory -ChildPath "..\..\src"

# 解析相对路径为绝对路径
$RootPath = Resolve-Path -Path $RootPath | Select-Object -ExpandProperty Path

# 检查目标目录是否存在
if (-Not (Test-Path -Path $RootPath)) {
    Write-Host "路径不存在: $RootPath"
    exit
}

Write-Host "开始清理目录：$RootPath"

# 查找并删除 bin 和 obj 文件夹
Get-ChildItem -Path $RootPath -Recurse -Directory -Force |
Where-Object { $_.Name -in "bin", "obj" } |
ForEach-Object {
    try {
        # 使用 \\?\ 处理长路径问题
        Remove-Item -Path "\\?\$($_.FullName)" -Recurse -Force
        # 延迟确保释放资源
        Start-Sleep -Milliseconds 100
        Write-Host "已删除: $($_.FullName)"
    }
    catch {
        Write-Host "删除失败: $($_.FullName), 错误: $($_.Exception.Message)"
    }
}

Write-Host "清理完成！"
