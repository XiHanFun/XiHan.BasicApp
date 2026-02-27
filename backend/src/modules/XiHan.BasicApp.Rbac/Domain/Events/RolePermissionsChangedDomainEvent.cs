using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 角色权限变更事件
/// </summary>
public sealed class RolePermissionsChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="roleId">角色 ID</param>
    /// <param name="permissionIds">新权限集合</param>
    public RolePermissionsChangedDomainEvent(long roleId, IReadOnlyCollection<long> permissionIds)
    {
        RoleId = roleId;
        PermissionIds = permissionIds;
    }

    /// <summary>
    /// 角色 ID
    /// </summary>
    public long RoleId { get; }

    /// <summary>
    /// 新权限集合
    /// </summary>
    public IReadOnlyCollection<long> PermissionIds { get; }
}
