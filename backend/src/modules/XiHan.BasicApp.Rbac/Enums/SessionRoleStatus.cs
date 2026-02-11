#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionRoleStatus
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 会话角色状态枚举
/// </summary>
public enum SessionRoleStatus
{
    /// <summary>
    /// 已激活
    /// </summary>
    Active = 0,

    /// <summary>
    /// 已停用
    /// </summary>
    Inactive = 1,

    /// <summary>
    /// 已过期
    /// </summary>
    Expired = 2
}
