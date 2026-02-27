#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantStatusChangedDomainEvent
// Guid:e54c90b7-d960-4216-968e-3d1882e3cb43
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:37:39
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 租户状态变更事件
/// </summary>
public sealed class TenantStatusChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="tenantId">租户 ID</param>
    /// <param name="tenantStatus">新状态</param>
    public TenantStatusChangedDomainEvent(long tenantId, TenantStatus tenantStatus)
    {
        TenantId = tenantId;
        TenantStatus = tenantStatus;
    }

    /// <summary>
    /// 租户 ID
    /// </summary>
    public long TenantId { get; }

    /// <summary>
    /// 新状态
    /// </summary>
    public TenantStatus TenantStatus { get; }
}
