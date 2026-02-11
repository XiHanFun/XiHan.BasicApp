#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleType
// Guid:9d28152c-d6e9-4396-addb-b479254bad29
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 04:20:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 角色类型枚举
/// </summary>
public enum RoleType
{
    /// <summary>
    /// 系统角色
    /// </summary>
    System = 0,

    /// <summary>
    /// 业务角色
    /// </summary>
    Business = 1,

    /// <summary>
    /// 自定义角色
    /// </summary>
    Custom = 2
}
