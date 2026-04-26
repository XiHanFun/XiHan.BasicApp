#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasDomainEventBase
// Guid:a71450da-7f58-4cd1-9237-6079fda96cfa
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
