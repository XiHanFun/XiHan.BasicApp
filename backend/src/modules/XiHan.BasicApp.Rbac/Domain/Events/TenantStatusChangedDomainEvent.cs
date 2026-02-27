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
