// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Clients;

/// <summary>
/// 从 User-Agent 粗分出的浏览器、操作系统与设备类型（日志与在线会话的展示列，不做精确识别；认不出即为 <see langword="null"/>）。
/// </summary>
/// <param name="Browser">浏览器。</param>
/// <param name="Os">操作系统。</param>
/// <param name="Device">设备类型：Desktop、Mobile、Tablet、Bot。</param>
public readonly record struct UserAgentInfo(string? Browser, string? Os, string? Device)
{
    /// <summary>
    /// 解析；没有 User-Agent 返回全空。次序有讲究：Edge / Opera 的串里也带 Chrome，Chrome 的串里也带 Safari，所以先认特征更具体的。
    /// </summary>
    public static UserAgentInfo Parse(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return default;
        }

        var ua = userAgent;
        string? browser = Contains(ua, "Edg/") ? "Edge"
            : Contains(ua, "OPR/") || Contains(ua, "Opera") ? "Opera"
            : Contains(ua, "Firefox/") ? "Firefox"
            : Contains(ua, "Chrome/") || Contains(ua, "CriOS/") ? "Chrome"
            : Contains(ua, "Safari/") ? "Safari"
            : Contains(ua, "curl/") ? "curl"
            : Contains(ua, "PostmanRuntime") ? "Postman"
            : null;
        string? os = Contains(ua, "Windows") ? "Windows"
            : Contains(ua, "Android") ? "Android"
            : Contains(ua, "iPhone") || Contains(ua, "iPad") || Contains(ua, "iOS") ? "iOS"
            : Contains(ua, "Mac OS X") || Contains(ua, "Macintosh") ? "macOS"
            : Contains(ua, "Linux") ? "Linux"
            : null;
        var device = Contains(ua, "bot") || Contains(ua, "spider") || Contains(ua, "crawler") ? "Bot"
            : Contains(ua, "iPad") || Contains(ua, "Tablet") ? "Tablet"
            : Contains(ua, "Mobi") || Contains(ua, "iPhone") || Contains(ua, "Android") ? "Mobile"
            : browser is not null && os is not null ? "Desktop"
            : null;
        return new UserAgentInfo(browser, os, device);
    }

    private static bool Contains(string value, string token) => value.Contains(token, StringComparison.OrdinalIgnoreCase);
}
