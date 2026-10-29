// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Pages;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包的页面登记（方案 D4）。
/// </summary>
public static class OpenPlatformPages
{
    /// <summary>
    /// 应用管理页（平台，4.x「开放平台 &gt; 应用管理」）。
    /// </summary>
    public static PageDescriptor OAuthApps { get; } = new("openplatform.oauth-apps", "/openplatform/oauth-apps", "openplatform.oauth_apps.title", "lucide:blocks", 600, OpenPlatformPermissions.OAuthAppsRead);

    /// <summary>
    /// 本包全部页面。
    /// </summary>
    public static IReadOnlyList<PageDescriptor> All { get; } = [OAuthApps];
}
