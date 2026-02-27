using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Events;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 租户聚合领域行为
/// </summary>
public partial class SysTenant
{
    /// <summary>
    /// 切换租户状态并发布事件
    /// </summary>
    public void ChangeTenantStatus(TenantStatus status)
    {
        TenantStatus = status;
        AddLocalEvent(new TenantStatusChangedDomainEvent(BasicId, status));
    }

    /// <summary>
    /// 启用租户
    /// </summary>
    public void Enable()
    {
        Status = YesOrNo.Yes;
    }

    /// <summary>
    /// 禁用租户
    /// </summary>
    public void Disable()
    {
        Status = YesOrNo.No;
        ChangeTenantStatus(TenantStatus.Disabled);
    }
}
