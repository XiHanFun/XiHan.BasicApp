#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysConstraintRule.Enum
// Guid:834775ae-05ce-4c4a-97e9-c81e247c2316
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 约束目标类型枚举
/// </summary>
public enum ConstraintTargetType
{
    /// <summary>
    /// 角色
    /// </summary>
    [Description("角色")]
    Role = 0,

    /// <summary>
    /// 权限
    /// </summary>
    [Description("权限")]
    Permission = 1,

    /// <summary>
    /// 用户
    /// </summary>
    [Description("用户")]
    User = 2
}

/// <summary>
/// 约束规则类型枚举
/// </summary>
public enum ConstraintType
{
    /// <summary>
    /// 静态职责分离（Static Separation of Duty）
    /// 用户不能同时拥有互斥的角色
    /// </summary>
    [Description("用户不能同时拥有互斥的角色")]
    SSD = 0,

    /// <summary>
    /// 动态职责分离（Dynamic Separation of Duty）
    /// 同一会话不能同时激活互斥的角色
    /// </summary>
    [Description("同一会话不能同时激活互斥的角色")]
    DSD = 1,

    /// <summary>
    /// 互斥约束
    /// 某些权限不能同时授予
    /// </summary>
    [Description("某些权限不能同时授予")]
    MutualExclusion = 2,

    /// <summary>
    /// 基数约束
    /// 限制角色或权限的数量
    /// </summary>
    [Description("限制角色或权限的数量")]
    Cardinality = 3,

    /// <summary>
    /// 先决条件约束
    /// 获得某角色/权限前必须先拥有其他角色/权限
    /// </summary>
    [Description("获得某角色/权限前必须先拥有其他角色/权限")]
    Prerequisite = 4,

    /// <summary>
    /// 时间约束
    /// 基于时间的访问控制
    /// </summary>
    [Description("基于时间的访问控制")]
    Temporal = 5,

    /// <summary>
    /// 位置约束
    /// 基于位置的访问控制
    /// </summary>
    [Description("基于位置的访问控制")]
    Location = 6,

    /// <summary>
    /// 自定义约束
    /// </summary>
    [Description("自定义约束")]
    Custom = 99
}

/// <summary>
/// 违规处理方式枚举
/// </summary>
public enum ViolationAction
{
    /// <summary>
    /// 拒绝操作
    /// </summary>
    [Description("拒绝操作")]
    Deny = 0,

    /// <summary>
    /// 警告但允许
    /// </summary>
    [Description("警告但允许")]
    Warning = 1,

    /// <summary>
    /// 仅记录日志
    /// </summary>
    [Description("仅记录日志")]
    Log = 2,

    /// <summary>
    /// 需要审批
    /// </summary>
    [Description("需要审批")]
    RequireApproval = 3
}
