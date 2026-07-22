// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Saas.Domain.Events;

/// <summary>
/// SaaS 领域事件基类
/// </summary>
public abstract class SaasDomainEventBase : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="operatorUserId">操作人ID</param>
    /// <param name="reason">原因</param>
    protected SaasDomainEventBase(long tenantId, long? operatorUserId, string? reason)
    {
        TenantId = tenantId;
        OperatorUserId = operatorUserId;
        Reason = reason;
    }

    /// <summary>
    /// 租户ID
    /// </summary>
    public long TenantId { get; }

    /// <summary>
    /// 操作人ID
    /// </summary>
    public long? OperatorUserId { get; }

    /// <summary>
    /// 原因
    /// </summary>
    public string? Reason { get; }
}
