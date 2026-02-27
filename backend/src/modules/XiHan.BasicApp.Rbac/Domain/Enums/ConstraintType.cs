#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConstraintType
// Guid:1a2b3c4d-5e6f-7890-abcd-ef1234567890
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/07 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Domain.Enums;

/// <summary>
/// 约束规则类型枚举
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// 静态职责分离（Static Separation of Duty）
    /// 用户不能同时拥有互斥的角色
    /// </summary>
    SSD = 0,

    /// <summary>
    /// 动态职责分离（Dynamic Separation of Duty）
    /// 同一会话不能同时激活互斥的角色
    /// </summary>
    DSD = 1,

    /// <summary>
    /// 互斥约束
    /// 某些权限不能同时授予
    /// </summary>
    MutualExclusion = 2,

    /// <summary>
    /// 基数约束
    /// 限制角色或权限的数量
    /// </summary>
    Cardinality = 3,

    /// <summary>
    /// 先决条件约束
    /// 获得某角色/权限前必须先拥有其他角色/权限
    /// </summary>
    Prerequisite = 4,

    /// <summary>
    /// 时间约束
    /// 基于时间的访问控制
    /// </summary>
    Temporal = 5,

    /// <summary>
    /// 位置约束
    /// 基于位置的访问控制
    /// </summary>
    Location = 6,

    /// <summary>
    /// 自定义约束
    /// </summary>
    Custom = 99
}
