#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright (c)2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysSessionRole.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 会话角色状态枚举
/// </summary>
public enum SessionRoleStatus
{
    /// <summary>
    /// 已激活
    /// </summary>
    [Description("已激活")]
    Active = 0,

    /// <summary>
    /// 已停用
    /// </summary>
    [Description("已停用")]
    Inactive = 1,

    /// <summary>
    /// 已过期
    /// </summary>
    [Description("已过期")]
    Expired = 2
}
