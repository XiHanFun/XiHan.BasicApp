// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Security.Abstractions.Conditions;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 访问策略（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 按请求属性（地址、归属地、时段、User-Agent、主体、路径等）只记录、拒绝或放行（方案 5.9）：权限码管「能做什么」，这一层管「从哪里、什么时候能进来」，所以在租户解析之后、授权之前判定，登录签发前也判一次
/// 落平台库，按 <see cref="OwnerTenantId"/> 分两层：平台层管全部外部调用（HTTP、gRPC、MCP），租户层只管本租户已登录的调用；平台层先判，拦下即不看租户层，租户的放行策略开不了平台的封锁
/// 层内放行压过拒绝：给白名单开口子只要加一条放行，不必逐条改拒绝策略；命中的策略全部记条件命中，只记录的策略也有留痕
/// 新建一律只记录、启用，动作与状态另经操作修改；改完仍是启用中的拒绝时按发起修改的请求试算，会拦下自己即拒绝保存：先观察命中再收紧，免得一条策略把管理员锁在外面，真锁住了由配置 <c>BasicApp:Access:AccessPolicyEnabled</c> 总开关关掉
/// 每次外部调用都要判定，所以一侧启用的策略连同条件（<see cref="SysAccessPolicyCondition"/>）放共享缓存 60 秒，写入后在工作单元结束时失效；有效期在判定时按当前时间比较，到点生效或失效不必等缓存过期
/// 编码同侧唯一是部分唯一索引，软删除后同一编码可以重建；<see cref="TemplateCode"/> 只记来自哪个代码里的预置模板，种子按模板补写平台侧还没有的编码，已有的（含改过的）不动
/// </remarks>
[SugarTable(TableName = "sys_access_policy", TableDescription = "访问策略（平台库实体，随所属侧）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(Code), Name = "ux_sys_access_policy_owner_tenant_id_code")]
public sealed class SysAccessPolicy : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 每条策略最多几个条件组
    /// </summary>
    public const int MaxGroups = 20;

    /// <summary>
    /// 每条策略最多几条条件
    /// </summary>
    public const int MaxConditions = 100;

    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 编码（同一侧唯一，不含空白）
    /// </summary>
    [SugarColumn(ColumnName = "code", Length = 100, IsNullable = false, ColumnDescription = "编码（同一侧唯一，不含空白）")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "name", Length = 200, IsNullable = false, ColumnDescription = "名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 1000, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }

    /// <summary>
    /// 命中时的动作
    /// </summary>
    [SugarColumn(ColumnName = "action", IsNullable = false, ColumnDescription = "命中时的动作")]
    public AccessPolicyAction Action { get; set; }

    /// <summary>
    /// 优先级（越小越先求值）
    /// </summary>
    [SugarColumn(ColumnName = "priority", IsNullable = false, ColumnDescription = "优先级（越小越先求值）")]
    public int Priority { get; set; } = 100;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public AccessPolicyStatus Status { get; set; } = AccessPolicyStatus.Active;

    /// <summary>
    /// 生效时间
    /// </summary>
    /// <remarks>
    /// 为空即不限
    /// </remarks>
    [SugarColumn(ColumnName = "effective_at", IsNullable = true, ColumnDescription = "生效时间")]
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 失效时间
    /// </summary>
    /// <remarks>
    /// 为空即不限
    /// </remarks>
    [SugarColumn(ColumnName = "expires_at", IsNullable = true, ColumnDescription = "失效时间")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 来自哪个预置模板
    /// </summary>
    [SugarColumn(ColumnName = "template_code", Length = 100, IsNullable = true, ColumnDescription = "来自哪个预置模板")]
    public string? TemplateCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }

    /// <summary>
    /// 某一时刻是否在有效期内
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}

/// <summary>
/// 访问策略的一条条件（平台库实体）：随策略整体替换，组内且、组间或
/// </summary>
/// <remarks>
/// 一条条件一行，<see cref="OwnerTenantId"/> 与策略相同：按侧与一批策略 ID 一起读，判定缓存、列表投影与防自锁试算都这样取
/// 没有单条的增删改，只随策略整组替换：先物理删掉这条策略的全部条件，再按提交顺序写入、<see cref="Ordinal"/> 记顺序，读回按它排、按组号组装成条件引擎的条件集
/// </remarks>
[SugarTable(TableName = "sys_access_policy_condition", TableDescription = "访问策略的一条条件（平台库实体）：随策略整体替换，组内且、组间或")]
[SugarIndex("ix_sys_access_policy_condition_owner_tenant_id_policy_id", nameof(OwnerTenantId), OrderByType.Asc, nameof(PolicyId), OrderByType.Asc)]
public sealed class SysAccessPolicyCondition : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 所属策略
    /// </summary>
    [SugarColumn(ColumnName = "policy_id", IsNullable = false, ColumnDescription = "所属策略")]
    public Guid PolicyId { get; set; }

    /// <summary>
    /// 写入顺序（读回时按它排）
    /// </summary>
    [SugarColumn(ColumnName = "ordinal", IsNullable = false, ColumnDescription = "写入顺序（读回时按它排）")]
    public int Ordinal { get; set; }

    /// <summary>
    /// 条件组号
    /// </summary>
    [SugarColumn(ColumnName = "group_no", IsNullable = false, ColumnDescription = "条件组号")]
    public int GroupNo { get; set; }

    /// <summary>
    /// 属性名
    /// </summary>
    [SugarColumn(ColumnName = "attribute_name", Length = 200, IsNullable = false, ColumnDescription = "属性名")]
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
    [SugarColumn(ColumnName = "value", Length = SysPermissionCondition.MaxValueLength, IsNullable = true, ColumnDescription = "条件值（判空类运算符不填）")]
    public string? Value { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnName = "description", Length = 500, IsNullable = true, ColumnDescription = "说明")]
    public string? Description { get; set; }
}

/// <summary>
/// 访问策略命中时的动作
/// </summary>
public enum AccessPolicyAction
{
    /// <summary>
    /// 只记录命中
    /// </summary>
    LogOnly = 0,

    /// <summary>
    /// 拒绝（同层有放行命中时不拒绝）
    /// </summary>
    Deny = 1,

    /// <summary>
    /// 放行（压过同层的拒绝）
    /// </summary>
    Allow = 2,
}

/// <summary>
/// 访问策略的状态
/// </summary>
public enum AccessPolicyStatus
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
