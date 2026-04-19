#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionCondition
// Guid:f6a7b8c9-d0e1-2345-f012-456789012306
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限 ABAC 条件实体
/// 为角色权限或用户直授权限附加属性条件，实现轻量级 ABAC
/// 同组条件为 AND 关系，不同组之间为 OR 关系
/// </summary>
/// <remarks>
/// 排他约束：RolePermissionId 和 UserPermissionId 必须恰好有一个非空。
/// 服务层写入时必须校验：(RolePermissionId != null) XOR (UserPermissionId != null)，
/// 两者同时为空 → 孤儿条件；两者同时非空 → 归属歧义。
/// 建议在数据库层补充 CHECK 约束（需通过迁移脚本手工添加）：
///   CHECK (RolePermissionId IS NOT NULL AND UserPermissionId IS NULL
///       OR RolePermissionId IS NULL AND UserPermissionId IS NOT NULL)
/// 迁移脚本/手工修数据等绕过服务层的操作也能得到数据库层保护。
///
/// 示例场景：
/// - 时间限制：AttributeName="environment.hour", Operator=Between, ConditionValue="9,18"（仅工作时间可用）
/// - 数据状态：AttributeName="resource.status", Operator=Equals, ConditionValue="draft"（只能操作草稿）
/// - IP限制：AttributeName="environment.ip", Operator=StartsWith, ConditionValue="192.168."（仅内网可用）
/// - 金额限制：AttributeName="resource.amount", Operator=LessThan, ConditionValue="10000"（限额操作）
///
/// 建模约定：
/// - AttributeName 只允许使用已注册的命名空间（subject./resource./environment.）
/// - ValueType 用于固定解释器如何解析 ConditionValue，避免同一属性在不同规则里出现值类型漂移
/// </remarks>
[SugarTable("SysPermissionCondition", "权限ABAC条件表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_RoPeId", nameof(RolePermissionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_UsPeId", nameof(UserPermissionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CoGr", nameof(ConditionGroup), OrderByType.Asc)]
[SugarIndex("IX_{table}_AtNa", nameof(AttributeName), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysPermissionCondition : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 角色权限关联ID（绑定到角色的权限时使用，与 UserPermissionId 二选一）
    /// </summary>
    [SugarColumn(ColumnDescription = "角色权限关联ID", IsNullable = true)]
    public virtual long? RolePermissionId { get; set; }

    /// <summary>
    /// 用户权限关联ID（绑定到用户直授权限时使用，与 RolePermissionId 二选一）
    /// </summary>
    [SugarColumn(ColumnDescription = "用户权限关联ID", IsNullable = true)]
    public virtual long? UserPermissionId { get; set; }

    /// <summary>
    /// 条件分组（同组内 AND，跨组 OR）
    /// </summary>
    [SugarColumn(ColumnDescription = "条件分组")]
    public virtual int ConditionGroup { get; set; } = 0;

    /// <summary>
    /// 属性名称（XACML 风格命名空间：subject.*/resource.*/environment.*）
    /// </summary>
    /// <remarks>
    /// 常用属性名：
    /// - subject.department: 主体所属部门
    /// - subject.role: 主体当前角色
    /// - resource.status: 资源状态
    /// - resource.owner: 资源创建者
    /// - resource.amount: 资源金额
    /// - environment.time: 当前时间
    /// - environment.hour: 当前小时
    /// - environment.ip: 请求IP
    /// - environment.location: 请求位置
    /// </remarks>
    [SugarColumn(ColumnDescription = "属性名称", Length = 200, IsNullable = false)]
    public virtual string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 操作符
    /// </summary>
    [SugarColumn(ColumnDescription = "操作符")]
    public virtual ConditionOperator Operator { get; set; } = ConditionOperator.Equals;

    /// <summary>
    /// 条件值类型
    /// </summary>
    [SugarColumn(ColumnDescription = "条件值类型")]
    public virtual ConfigDataType ValueType { get; set; } = ConfigDataType.String;

    /// <summary>
    /// 条件值（JSON格式，支持简单值和复杂类型）
    /// </summary>
    /// <remarks>
    /// 示例：
    /// - 简单值："draft"、"10000"
    /// - 集合值：["east","west"]
    /// - 范围值：{"min":9,"max":18}
    /// </remarks>
    [SugarColumn(ColumnDescription = "条件值", Length = 1000, IsNullable = false)]
    public virtual string ConditionValue { get; set; } = string.Empty;

    /// <summary>
    /// 条件说明
    /// </summary>
    [SugarColumn(ColumnDescription = "条件说明", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
