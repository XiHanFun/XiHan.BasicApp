// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 授予规则（平台库实体，随主体分侧）
/// </summary>
/// <remarks>
/// 一个主体（角色或用户）在一个权限码上的 Grant 附加信息或 Deny：授予本身仍写框架的 <c>PermissionGrantRecord</c>（别的包直接写入的授予同样算数），这里只放框架记录表达不了的——Deny（不写框架授予）、授予的生效与到期时间、原因
/// 普通授予（没有有效期也没有原因）不留规则行，把授予改回普通即删掉它的行：本表只装例外，框架存储仍是授予的真源
/// 判定经 <c>AccessPermissionFilter</c> 在框架的授予命中之后按「用户 Deny &gt; 用户 Grant &gt; 角色 Deny（本角色）&gt; 角色 Grant」收窄：用户上的拒绝压过一切，角色上的拒绝只拿走这个角色自己的授予；<c>auth.permissions</c> 的有效权限同源
/// 落平台库，<see cref="OwnerTenantId"/> 随主体所在的一侧；同一侧同一主体同一权限码只有一行，已有的就地覆盖动作、有效期与原因。不软删除，撤销即物理删除，所以是普通唯一索引
/// 判定按（侧、主体）读全部规则放共享缓存 60 秒、写入后失效；有效期在判定时按当前时间比较，到点生效与到期不必重建缓存
/// </remarks>
[SugarTable(TableName = "sys_permission_rule", TableDescription = "授予规则（平台库实体，随主体分侧）")]
[SugarIndex("ux_sys_permission_rule_owner_subject_permission", nameof(OwnerTenantId), OrderByType.Asc, nameof(SubjectKind), OrderByType.Asc, nameof(SubjectId), OrderByType.Asc, nameof(Permission), OrderByType.Asc, true)]
public sealed class SysPermissionRule : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    [SugarColumn(ColumnName = "owner_tenant_id", Length = 512, IsNullable = false, ColumnDescription = "属主租户 ID")]
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 主体类型
    /// </summary>
    [SugarColumn(ColumnName = "subject_kind", IsNullable = false, ColumnDescription = "主体类型")]
    public GrantSubjectKind SubjectKind { get; set; }

    /// <summary>
    /// 主体 ID（角色 ID 或用户 ID）
    /// </summary>
    [SugarColumn(ColumnName = "subject_id", IsNullable = false, ColumnDescription = "主体 ID（角色 ID 或用户 ID）")]
    public Guid SubjectId { get; set; }

    /// <summary>
    /// 权限码（或 <c>*</c>）
    /// </summary>
    [SugarColumn(ColumnName = "permission", Length = 128, IsNullable = false, ColumnDescription = "权限码（或 *）")]
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// 授予还是拒绝
    /// </summary>
    [SugarColumn(ColumnName = "action", IsNullable = false, ColumnDescription = "授予还是拒绝")]
    public PermissionAction Action { get; set; }

    /// <summary>
    /// 生效时间
    /// </summary>
    /// <remarks>
    /// 为空即立即生效
    /// </remarks>
    [SugarColumn(ColumnName = "effective_at", IsNullable = true, ColumnDescription = "生效时间")]
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（不含）
    /// </summary>
    /// <remarks>
    /// 为空即长期有效
    /// </remarks>
    [SugarColumn(ColumnName = "expires_at", IsNullable = true, ColumnDescription = "到期时间（不含）")]
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 原因
    /// </summary>
    [SugarColumn(ColumnName = "reason", Length = 512, IsNullable = true, ColumnDescription = "原因")]
    public string? Reason { get; set; }

    /// <summary>
    /// 此刻是否在有效期内
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}
