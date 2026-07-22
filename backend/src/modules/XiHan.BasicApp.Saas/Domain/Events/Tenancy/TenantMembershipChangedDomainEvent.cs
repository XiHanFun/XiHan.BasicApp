// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 租户成员变更事件
/// </summary>
public sealed class TenantMembershipChangedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantMembershipChangedDomainEvent(
        long tenantId,
        long userId,
        TenantMemberInviteStatus inviteStatus,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        UserId = userId;
        InviteStatus = inviteStatus;
    }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 邀请状态
    /// </summary>
    public TenantMemberInviteStatus InviteStatus { get; }
}
