using XiHan.Framework.Domain.Events;

namespace XiHan.BasicApp.Rbac.Domain.Events;

/// <summary>
/// 用户密码变更事件
/// </summary>
public sealed class UserPasswordChangedDomainEvent : DomainEventBase
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userId">用户 ID</param>
    public UserPasswordChangedDomainEvent(long userId)
    {
        UserId = userId;
    }

    /// <summary>
    /// 用户 ID
    /// </summary>
    public long UserId { get; }
}
