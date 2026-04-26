#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantStatusChangedDomainEvent
// Guid:1f3c287e-0b41-40c2-9ad5-682a0c3e073b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
