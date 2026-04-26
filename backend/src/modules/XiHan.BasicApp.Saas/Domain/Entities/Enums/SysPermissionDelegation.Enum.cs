#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionDelegation.Enum
// Guid:9ab7ecd1-8f23-4651-aaa9-d498fcb8d13a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限委托状态枚举
/// </summary>
public enum DelegationStatus
{
    /// <summary>
    /// 待生效
    /// </summary>
    [Description("待生效")]
    Pending = 0,

    /// <summary>
    /// 生效中
    /// </summary>
    [Description("生效中")]
    Active = 1,

    /// <summary>
    /// 已过期
    /// </summary>
    [Description("已过期")]
    Expired = 2,

    /// <summary>
    /// 已撤销
    /// </summary>
    [Description("已撤销")]
    Revoked = 3
}
