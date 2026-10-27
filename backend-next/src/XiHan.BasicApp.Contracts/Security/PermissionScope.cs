// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 权限的作用侧：平台主体与租户主体分别只能命中各自作用侧的权限。
/// </summary>
public enum PermissionScope
{
    /// <summary>只有平台主体可用。</summary>
    Platform = 0,

    /// <summary>只有租户主体可用。</summary>
    Tenant = 1,

    /// <summary>两侧都可用。</summary>
    Both = 2,
}
