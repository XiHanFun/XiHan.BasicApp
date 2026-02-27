using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户创建事件
/// </summary>
public sealed class UserCreatedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    /// <param name="userName">用户名</param>
    public UserCreatedDomainEvent(long userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; }
}
