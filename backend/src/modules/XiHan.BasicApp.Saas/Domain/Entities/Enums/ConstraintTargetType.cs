#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintTargetType
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567801
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Entities.Enums;

/// <summary>
/// 约束目标类型枚举
/// </summary>
public enum ConstraintTargetType
{
    /// <summary>
    /// 角色
    /// </summary>
    Role = 0,

    /// <summary>
    /// 权限
    /// </summary>
    Permission = 1,

    /// <summary>
    /// 用户
    /// </summary>
    User = 2
}
