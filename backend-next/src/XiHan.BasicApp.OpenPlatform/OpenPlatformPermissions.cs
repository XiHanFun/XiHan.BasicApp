// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包的权限码：OAuth 应用只在平台一侧管理（4.x 同）。签名开放接口不走权限（调用方不成为主体），我的应用是自助的、只要求已登录，都不在这里。
/// </summary>
public static class OpenPlatformPermissions
{
    /// <summary>
    /// 查看 OAuth 应用。
    /// </summary>
    public const string OAuthAppsRead = "openplatform.oauth-apps.read";

    /// <summary>
    /// 新建 OAuth 应用。
    /// </summary>
    public const string OAuthAppsCreate = "openplatform.oauth-apps.create";

    /// <summary>
    /// 修改 OAuth 应用（含启停与重置密钥）。
    /// </summary>
    public const string OAuthAppsUpdate = "openplatform.oauth-apps.update";

    /// <summary>
    /// 删除 OAuth 应用。
    /// </summary>
    public const string OAuthAppsDelete = "openplatform.oauth-apps.delete";

    /// <summary>
    /// 本包全部权限码。
    /// </summary>
    public static IReadOnlyList<PermissionDescriptor> All { get; } =
    [
        new(OAuthAppsRead, "Read OAuth apps", PermissionScope.Platform, description: "View the OAuth apps created by the platform and by users; client secrets are never shown."),
        new(OAuthAppsCreate, "Create OAuth apps", PermissionScope.Platform, audited: true, description: "Create OAuth apps for single sign-on."),
        new(OAuthAppsUpdate, "Update OAuth apps", PermissionScope.Platform, audited: true, description: "Edit, enable and disable OAuth apps and reset their client secrets."),
        new(OAuthAppsDelete, "Delete OAuth apps", PermissionScope.Platform, audited: true, description: "Delete OAuth apps; tokens issued to them stop working."),
    ];
}
