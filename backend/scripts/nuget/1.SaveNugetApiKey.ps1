# 检查环境变量是否已存在
$apiKey = [System.Environment]::GetEnvironmentVariable("NUGET_API_KEY", "User")

# 如果环境变量不存在，提示用户输入并保存
if (-not $apiKey) {
    # 提示用户输入 NuGet API 密钥
    $apiKeySecure = Read-Host -AsSecureString "请输入 NuGet API 密钥"
    # 将 SecureString 转为纯文本
    $apiKeyPlainText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKeySecure))
    # 设置环境变量
    [System.Environment]::SetEnvironmentVariable("NUGET_API_KEY", $apiKeyPlainText, "User")
    # 从环境变量读取 API 密钥，以便后续使用
    $apiKey = $apiKeyPlainText
    Write-Output "NuGet API 密钥已成功保存到环境变量。"
}
else {
    Write-Output "已检测到环境变量中的 NuGet API 密钥。"
    # 是否确认覆盖
    $confirmOverwrite = Read-Host "是否确认覆盖 (Y|y / N|n)"
    if ($confirmOverwrite -eq "Y") {
        # 提示用户输入 NuGet API 密钥
        $apiKeySecure = Read-Host -AsSecureString "请输入 NuGet API 密钥"
        # 将 SecureString 转为纯文本
        $apiKeyPlainText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($apiKeySecure))
        # 设置环境变量
        [System.Environment]::SetEnvironmentVariable("NUGET_API_KEY", $apiKeyPlainText, "User")
        # 从环境变量读取 API 密钥，以便后续使用
        $apiKey = $apiKeyPlainText
        Write-Output "NuGet API 密钥已成功覆盖。"
    }
    else {
        Write-Output "已取消覆盖。"
    }
}
