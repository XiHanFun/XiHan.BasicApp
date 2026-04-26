#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantMembershipChangedDomainEvent
// Guid:a24f8d66-06f9-478b-b2d3-5caa7951bb95
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
