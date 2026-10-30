// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户成员（平台库，表 <c>sys_tenant_member</c>）。
/// </summary>
/// <remarks>
/// 注册地不在该租户的用户以成员身份进入它（切换租户、在其中被模仿）。
/// 一个用户在一个租户里至多一行；注册地在该租户的用户天然可进入，不记成员行。成员关系只在生效期内有效（照 4.x）。
/// </remarks>
public sealed class SysTenantMember : AppAuditedEntity
{
    /// <summary>
    /// 租户 ID（目录键）。
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 用户 ID。
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// 成员类型。
    /// </summary>
    public TenantMemberType MemberType { get; set; } = TenantMemberType.Member;

    /// <summary>
    /// 生效时间（UTC）。
    /// </summary>
    /// <remarks>
    /// 为空即加入即生效。
    /// </remarks>
    public DateTimeOffset? EffectiveAt { get; set; }

    /// <summary>
    /// 到期时间（UTC）。
    /// </summary>
    /// <remarks>
    /// 为空即不到期；到期后不能再进入该租户。
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 在 <paramref name="now"/> 是否生效。
    /// </summary>
    public bool IsInEffect(DateTimeOffset now) => (EffectiveAt is null || EffectiveAt <= now) && (ExpiresAt is null || ExpiresAt > now);
}
