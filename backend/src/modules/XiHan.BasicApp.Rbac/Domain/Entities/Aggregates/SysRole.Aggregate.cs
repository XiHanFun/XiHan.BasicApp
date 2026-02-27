using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 角色聚合领域行为
/// </summary>
public partial class SysRole
{
    /// <summary>
    /// 启用角色
    /// </summary>
    public void Enable()
    {
        Status = YesOrNo.Yes;
    }

    /// <summary>
    /// 禁用角色
    /// </summary>
    public void Disable()
    {
        Status = YesOrNo.No;
    }

    /// <summary>
    /// 记录权限变更
    /// </summary>
    public void MarkPermissionsChanged(IReadOnlyCollection<long> permissionIds)
    {
        AddLocalEvent(new RolePermissionsChangedDomainEvent(BasicId, permissionIds));
    }
}
