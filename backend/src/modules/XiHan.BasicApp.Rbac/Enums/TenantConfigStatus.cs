#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantConfigStatus
// Guid:1a28152c-d6e9-4396-addb-b479254bad90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 09:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 租户配置状态
/// </summary>
public enum TenantConfigStatus
{
    /// <summary>
    /// 待配置
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 配置中
    /// </summary>
    Configuring = 1,

    /// <summary>
    /// 已配置
    /// </summary>
    Configured = 2,

    /// <summary>
    /// 配置失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已停用
    /// </summary>
    Disabled = 4
}
