using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户角色变更事件
/// </summary>
public sealed class UserRolesChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="roleIds">新角色集合</param>
    public UserRolesChangedDomainEvent(long userId, IReadOnlyCollection<long> roleIds)
    {
        UserId = userId;
        RoleIds = roleIds;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 新角色集合
    /// </summary>
    public IReadOnlyCollection<long> RoleIds { get; }
}
