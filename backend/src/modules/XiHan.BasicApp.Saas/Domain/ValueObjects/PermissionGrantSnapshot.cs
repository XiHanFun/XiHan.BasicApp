#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionGrantSnapshot
// Guid:0853d8a5-0cf4-4c9a-af5d-bff8c510d6f1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.ValueObjects;

/// <summary>
/// 权限授予快照
/// </summary>
public sealed record PermissionGrantSnapshot(
    long PermissionId,
    string PermissionCode,
    PermissionAction Action,
    AuthorizationGrantSource Source,
    int Priority,
    EffectivePeriod Period,
    bool IsEnabled = true);

/// <summary>
/// 权限裁决结果
/// </summary>
public sealed record AuthorizationDecision(bool IsGranted, string PermissionCode, string Reason, long? PermissionId = null)
{
    /// <summary>
    /// 授权通过
    /// </summary>
    public static AuthorizationDecision Grant(string permissionCode, string reason, long? permissionId = null)
    {
        return new AuthorizationDecision(true, permissionCode, reason, permissionId);
    }

    /// <summary>
    /// 授权拒绝
    /// </summary>
    public static AuthorizationDecision Deny(string permissionCode, string reason, long? permissionId = null)
    {
        return new AuthorizationDecision(false, permissionCode, reason, permissionId);
    }
}
