// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// 租户状态变更事件
/// </summary>
public sealed class TenantStatusChangedDomainEvent : SaasDomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public TenantStatusChangedDomainEvent(
        long tenantId,
        long affectedTenantId,
        TenantStatus oldStatus,
        TenantStatus newStatus,
        long? operatorUserId = null,
        string? reason = null)
        : base(tenantId, operatorUserId, reason)
    {
        AffectedTenantId = affectedTenantId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }

    /// <summary>
    /// 受影响租户ID
    /// </summary>
    public long AffectedTenantId { get; }

    /// <summary>
    /// 原状态
    /// </summary>
    public TenantStatus OldStatus { get; }

    /// <summary>
    /// 新状态
    /// </summary>
    public TenantStatus NewStatus { get; }
}
