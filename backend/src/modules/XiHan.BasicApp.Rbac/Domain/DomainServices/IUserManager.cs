using XiHan.BasicApp.Rbac.Entities;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices;

/// <summary>
/// 用户领域管理器
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// 创建用户
    /// </summary>
    Task<SysUser> CreateAsync(SysUser user, string plainPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改用户密码
    /// </summary>
    Task ChangePasswordAsync(SysUser user, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验用户名是否可用
    /// </summary>
    Task EnsureUserNameUniqueAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
