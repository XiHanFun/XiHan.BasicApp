using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Events;
using XiHan.BasicApp.Rbac.Domain.ValueObjects;

namespace XiHan.BasicApp.Rbac.Domain.Entities;

/// <summary>
/// 用户聚合领域行为
/// </summary>
public partial class SysUser
{
    /// <summary>
    /// 标记用户创建完成并发布事件
    /// </summary>
    public void MarkCreated()
    {
        CreatedTime = DateTimeOffset.UtcNow;
        AddLocalEvent(new UserCreatedDomainEvent(BasicId, UserName));
    }

    /// <summary>
    /// 启用用户
    /// </summary>
    public void Enable()
    {
        Status = YesOrNo.Yes;
    }

    /// <summary>
    /// 禁用用户
    /// </summary>
    public void Disable()
    {
        Status = YesOrNo.No;
    }

    /// <summary>
    /// 更新密码
    /// </summary>
    public void ChangePassword(PasswordValueObject password)
    {
        Password = password.Hash;
        AddLocalEvent(new UserPasswordChangedDomainEvent(BasicId));
    }

    /// <summary>
    /// 记录角色变更
    /// </summary>
    public void MarkRolesChanged(IReadOnlyCollection<long> roleIds)
    {
        AddLocalEvent(new UserRolesChangedDomainEvent(BasicId, roleIds));
    }
}
