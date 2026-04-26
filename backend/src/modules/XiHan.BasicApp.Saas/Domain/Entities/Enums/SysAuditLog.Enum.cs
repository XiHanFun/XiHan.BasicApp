#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLog.Enum
// Guid:ed9f7f77-64a1-4f8d-b5f9-35fa37247dbb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 审计风险等级枚举（数字越大风险越高）
/// </summary>
public enum AuditRiskLevel
{
    /// <summary>
    /// 低风险（常规查询、列表浏览等）
    /// </summary>
    [Description("低风险（常规查询、列表浏览等）")]
    Low = 1,

    /// <summary>
    /// 中风险（数据修改、配置变更等）
    /// </summary>
    [Description("中风险（数据修改、配置变更等）")]
    Medium = 2,

    /// <summary>
    /// 高风险（权限变更、角色分配等）
    /// </summary>
    [Description("高风险（权限变更、角色分配等）")]
    High = 3,

    /// <summary>
    /// 极高风险（批量删除、数据导出等）
    /// </summary>
    [Description("极高风险（批量删除、数据导出等）")]
    VeryHigh = 4,

    /// <summary>
    /// 严重风险（超管操作、租户配置变更、安全策略修改等）
    /// </summary>
    [Description("严重风险（超管操作、租户配置变更、安全策略修改等）")]
    Critical = 5
}

/// <summary>
/// 操作类型枚举（审计日志专用，记录用户执行了什么类型的操作）
/// 注意：勿与 <see cref="OperationTypeCode"/> 混淆，后者用于 RBAC 权限模型中 SysOperation 的操作定义
/// </summary>
public enum OperationType
{
    /// <summary>
    /// 登录
    /// </summary>
    [Description("登录")]
    Login = 0,

    /// <summary>
    /// 登出
    /// </summary>
    [Description("登出")]
    Logout = 1,

    /// <summary>
    /// 查询
    /// </summary>
    [Description("查询")]
    Query = 2,

    /// <summary>
    /// 新增
    /// </summary>
    [Description("新增")]
    Create = 3,

    /// <summary>
    /// 修改
    /// </summary>
    [Description("修改")]
    Update = 4,

    /// <summary>
    /// 删除
    /// </summary>
    [Description("删除")]
    Delete = 5,

    /// <summary>
    /// 导入
    /// </summary>
    [Description("导入")]
    Import = 6,

    /// <summary>
    /// 导出
    /// </summary>
    [Description("导出")]
    Export = 7,

    /// <summary>
    /// 其他
    /// </summary>
    [Description("其他")]
    Other = 99
}
