// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Contracts.Security;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 字段安全规则（平台库实体，随所属侧）
/// </summary>
/// <remarks>
/// 一个目标（角色、用户或部门）在一个实体字段上的读取方式与能否改写（方案 5.9）：权限码只管能不能调操作，同一个接口对不同的人露出多少字段、能改哪些字段由这张表决定
/// 实体与字段按各包登记进字段安全目录的实体码与接口字段名引用，不指向表列：规则作用在请求与响应的契约字段上，由 <c>FieldSecurityMiddleware</c> 与响应过滤器在各包绑定的读、建、改操作上统一套用，各包不各写一遍
/// 落平台库，规则只在所属一侧（<see cref="OwnerTenantId"/>）生效：平台规则管平台主体，租户规则管本租户的主体；部门只在租户侧
/// 角色目标只认此刻直接持有的启用角色，不沿继承与委托传；部门目标认所在部门及其上级，所以给上级部门定的规则管到下级部门的成员
/// 同一侧同一目标同一字段只有一条，是部分唯一索引，软删除后可以重建；一个主体命中多条时按创建先后合并，固定文本取第一条
/// 每个绑定操作都要取规则，所以一侧一个实体上的启用规则放共享缓存 60 秒，写入后在工作单元结束时失效，换了实体即新旧两个都失效
/// 删角色时连同删掉以它为目标的规则；用户与部门删除不连带清理
/// </remarks>
[SugarTable(TableName = "sys_field_security_rule", TableDescription = "字段安全规则（平台库实体，随所属侧）")]
[ActiveUniqueIndex(nameof(OwnerTenantId), nameof(TargetKind), nameof(TargetId), nameof(Entity), nameof(Field), Name = "ux_sys_field_security_rule_target_field")]
[SugarIndex("ix_sys_field_security_rule_owner_tenant_id_entity_status", nameof(OwnerTenantId), OrderByType.Asc, nameof(Entity), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public sealed class SysFieldSecurityRule : AppAuditedEntity, IOwnedByTenant
{
    /// <summary>
    /// 部分脱敏保留位数的上限
    /// </summary>
    public const int MaxKeep = 32;

    /// <summary>
    /// 固定文本的长度上限
    /// </summary>
    public const int MaxReplacementLength = 100;

    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型
    /// </summary>
    [SugarColumn(ColumnName = "target_kind", IsNullable = false, ColumnDescription = "目标类型")]
    public FieldSecurityTargetKind TargetKind { get; set; }

    /// <summary>
    /// 目标 ID（角色、用户或部门）
    /// </summary>
    [SugarColumn(ColumnName = "target_id", IsNullable = false, ColumnDescription = "目标 ID（角色、用户或部门）")]
    public Guid TargetId { get; set; }

    /// <summary>
    /// 实体码（在字段安全目录里登记过，如 <c>identity.users</c>）
    /// </summary>
    [SugarColumn(ColumnName = "entity", Length = 64, IsNullable = false, ColumnDescription = "实体码（在字段安全目录里登记过，如 identity.users）")]
    public string Entity { get; set; } = string.Empty;

    /// <summary>
    /// 字段名（与接口字段同名）
    /// </summary>
    [SugarColumn(ColumnName = "field", Length = 64, IsNullable = false, ColumnDescription = "字段名（与接口字段同名）")]
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 读取方式
    /// </summary>
    [SugarColumn(ColumnName = "strategy", IsNullable = false, ColumnDescription = "读取方式")]
    public FieldMaskStrategy Strategy { get; set; }

    /// <summary>
    /// 部分脱敏保留前几位
    /// </summary>
    [SugarColumn(ColumnName = "keep_head", IsNullable = true, ColumnDescription = "部分脱敏保留前几位")]
    public int? KeepHead { get; set; }

    /// <summary>
    /// 部分脱敏保留后几位
    /// </summary>
    [SugarColumn(ColumnName = "keep_tail", IsNullable = true, ColumnDescription = "部分脱敏保留后几位")]
    public int? KeepTail { get; set; }

    /// <summary>
    /// 固定文本
    /// </summary>
    [SugarColumn(ColumnName = "replacement", Length = MaxReplacementLength, IsNullable = true, ColumnDescription = "固定文本")]
    public string? Replacement { get; set; }

    /// <summary>
    /// 能否改写
    /// </summary>
    /// <remarks>
    /// 明文规则只能只读；脱敏且可改即只写
    /// </remarks>
    [SugarColumn(ColumnName = "is_editable", IsNullable = false, ColumnDescription = "能否改写")]
    public bool IsEditable { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public FieldRuleStatus Status { get; set; } = FieldRuleStatus.Active;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 512, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}

/// <summary>
/// 字段安全规则的目标类型
/// </summary>
public enum FieldSecurityTargetKind
{
    /// <summary>
    /// 角色：此刻持有该角色（指派有效、角色启用）的用户
    /// </summary>
    /// <remarks>
    /// 只认直接持有
    /// </remarks>
    Role = 0,

    /// <summary>
    /// 用户本人
    /// </summary>
    User = 1,

    /// <summary>
    /// 部门：该部门及其下级部门的成员
    /// </summary>
    /// <remarks>
    /// 只在租户侧；停用的部门不算
    /// </remarks>
    Department = 2,
}

/// <summary>
/// 字段安全规则的状态
/// </summary>
public enum FieldRuleStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用：不参与判定
    /// </summary>
    Disabled = 1,
}
