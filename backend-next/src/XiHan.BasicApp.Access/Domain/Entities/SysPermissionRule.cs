// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Access.Domain.Entities;

/// <summary>
/// 授予规则（平台库实体，随主体分侧）。
/// </summary>
/// <remarks>
/// 一个主体（角色或用户）在一个权限码上的 Grant 附加信息或 Deny。同一侧同一主体同一权限码只有一行。
/// 授予本身仍写框架的 <c>PermissionGrantRecord</c>（别的包直接写入的授予同样算数），这里只放框架记录表达不了的：
/// Deny（不写框架授予）、授予的生效与到期时间、原因；没有规则行的授予就是普通授予。判定经 Access 的权限过滤器按「用户 Deny &gt; 用户 Grant &gt; 角色 Deny（本角色）&gt; 角色 Grant」收窄。
/// </remarks>
[SugarIndex("ux_{table}_owner_subject_permission", nameof(OwnerTenantId), OrderByType.Asc, nameof(SubjectKind), OrderByType.Asc, nameof(SubjectId), OrderByType.Asc, nameof(Permission), OrderByType.Asc, true)]
public sealed class SysPermissionRule : AppRecord, IOwnedByTenant
{
    /// <inheritdoc />
    public string OwnerTenantId { get; set; } = string.Empty;

    /// <summary>
    /// 主体类型。
    /// </summary>
    public GrantSubjectKind SubjectKind { get; set; }

    /// <summary>
    /// 主体 ID（角色 ID 或用户 ID）。
    /// </summary>
    public Guid SubjectId { get; set; }

    /// <summary>
    /// 权限码（或 <c>*</c>）。
    /// </summary>
    [StringLength(128)]
    public string Permission { get; set; } = string.Empty;

    /// <summary>
    /// 授予还是拒绝。
    /// </summary>
    public PermissionAction Action { get; set; }

    /// <summary>
    /// 生效时间。
    /// </summary>
    /// <remarks>
    /// 为空即立即生效。
    /// </remarks>
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（不含）。
    /// </summary>
    /// <remarks>
    /// 为空即长期有效。
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 原因。
    /// </summary>
    [StringLength(512)]
    public string? Reason { get; set; }

    /// <summary>
    /// 此刻是否在有效期内。
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}
