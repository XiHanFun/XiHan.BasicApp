// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 权限条件的一条子句（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 方案 5.9 的 ABAC：给一个授权绑定（侧 × 主体 × 权限码 × 授予或拒绝）加按请求属性判定的条件；同一绑定里同组的子句取且、组之间取或，挂在授予上是「满足才授予」，挂在拒绝上是「满足才拒绝」
/// 绑定按键直接对应，不经 <see cref="SysPermissionRule"/>：普通授予只在框架的授予存储里、没有规则行，按（主体、权限码、动作）挂才覆盖得到；绑定没了（撤销、授予与拒绝互换、删角色）即连同删除，不留悬空的条件
/// 框架的权限过滤器拿不到请求属性，所以条件不在框架判定里求值，而由框架授权之后的 <c>PermissionConditionMiddleware</c> 按调用来源、主体与时刻判定操作声明的权限码；配置 <c>BasicApp:Access:PermissionConditionMode</c> 可以只记录不拦
/// 一条子句一行、可单独启停：停用的子句不参与判定，启用时重新校验绑定；值种类由属性登记决定，不另存值类型
/// 判定按（侧、主体）取启用的子句，放共享缓存 60 秒、写入后在工作单元结束时失效；索引正按侧与主体建
/// </remarks>
[SugarTable(TableName = "sys_permission_condition", TableDescription = "权限条件的一条子句（平台库实体，随所属侧）")]
[SugarIndex("ix_sys_permission_condition_owner_subject", nameof(OwnerTenantId), OrderByType.Asc, nameof(SubjectKind), OrderByType.Asc, nameof(SubjectId), OrderByType.Asc)]
public sealed class SysPermissionCondition : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 每个绑定最多几组
    /// </summary>
    public const int MaxGroups = 5;

    /// <summary>
    /// 每组最多几条
    /// </summary>
    public const int MaxClausesPerGroup = 10;

    /// <summary>
    /// 条件值的长度上限
    /// </summary>
    public const int MaxValueLength = 2000;

    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 主体类别（角色或用户）
    /// </summary>
    [SugarColumn(ColumnName = "subject_kind", IsNullable = false, ColumnDescription = "主体类别（角色或用户）")]
    public GrantSubjectKind SubjectKind { get; set; }

    /// <summary>
    /// 主体 ID
    /// </summary>
    [SugarColumn(ColumnName = "subject_id", IsNullable = false, ColumnDescription = "主体 ID")]
    public Guid SubjectId { get; set; }

    /// <summary>
    /// 权限码
    /// </summary>
    [SugarColumn(ColumnName = "permission", Length = 128, IsNullable = false, ColumnDescription = "权限码")]
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// 挂在授予上还是拒绝上
    /// </summary>
    [SugarColumn(ColumnName = "action", IsNullable = false, ColumnDescription = "挂在授予上还是拒绝上")]
    public PermissionAction Action { get; set; }

    /// <summary>
    /// 条件组号（同组取且，组间取或）
    /// </summary>
    [SugarColumn(ColumnName = "group_no", IsNullable = false, ColumnDescription = "条件组号（同组取且，组间取或）")]
    public int GroupNo { get; set; }

    /// <summary>
    /// 属性名（登记名、别名或族名，如 <c>environment.ip</c>）
    /// </summary>
    [SugarColumn(ColumnName = "attribute_name", Length = 200, IsNullable = false, ColumnDescription = "属性名（登记名、别名或族名，如 environment.ip）")]
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 运算符
    /// </summary>
    [SugarColumn(ColumnName = "operator", IsNullable = false, ColumnDescription = "运算符")]
    public ConditionOperator Operator { get; set; }

    /// <summary>
    /// 是否取反
    /// </summary>
    [SugarColumn(ColumnName = "is_negated", IsNullable = false, ColumnDescription = "是否取反")]
    public bool IsNegated { get; set; }

    /// <summary>
    /// 条件值（判空类运算符不填）
    /// </summary>
    [SugarColumn(ColumnName = "value", Length = MaxValueLength, IsNullable = true, ColumnDescription = "条件值（判空类运算符不填）")]
    public string? Value { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 状态：停用的子句不参与判定
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态：停用的子句不参与判定")]
    public ConditionStatus Status { get; set; } = ConditionStatus.Active;
}

/// <summary>
/// 权限条件子句的状态
/// </summary>
public enum ConditionStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用
    /// </summary>
    Disabled = 1,
}
