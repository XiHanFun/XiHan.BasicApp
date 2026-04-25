#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuditRiskLevel
// Guid:a1b2c3d4-5e6f-7890-abcd-ef1234567899
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/25 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Enums;

/// <summary>
/// 审计风险等级枚举（数字越大风险越高）
/// </summary>
public enum AuditRiskLevel
{
    /// <summary>
    /// 低风险（常规查询、列表浏览等）
    /// </summary>
    Low = 1,

    /// <summary>
    /// 中风险（数据修改、配置变更等）
    /// </summary>
    Medium = 2,

    /// <summary>
    /// 高风险（权限变更、角色分配等）
    /// </summary>
    High = 3,

    /// <summary>
    /// 极高风险（批量删除、数据导出等）
    /// </summary>
    VeryHigh = 4,

    /// <summary>
    /// 严重风险（超管操作、租户配置变更、安全策略修改等）
    /// </summary>
    Critical = 5
}
