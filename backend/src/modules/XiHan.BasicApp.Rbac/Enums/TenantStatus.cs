#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantStatus
// Guid:19bf8a17-caed-4263-ba4b-fb941ceaea5a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:45:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 租户状态枚举
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 暂停
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// 过期
    /// </summary>
    Expired = 2,

    /// <summary>
    /// 禁用
    /// </summary>
    Disabled = 3
}
