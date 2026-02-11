#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SessionRoleStatus
// Guid:a5cc00f6-08bc-4a43-b112-5c9fbfd67f89
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
