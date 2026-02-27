using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.ValueObjects;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Authentication.Password;

namespace XiHan.BasicApp.Rbac.Domain.DomainServices.Implementations;

/// <summary>
/// 用户领域管理器实现
/// </summary>
public class UserManager : IUserManager
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSecurityRepository _userSecurityRepository;
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository">用户仓储</param>
    /// <param name="userSecurityRepository">用户安全仓储</param>
    /// <param name="passwordHasher">密码哈希器</param>
    public UserManager(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _passwordHasher = passwordHasher;
    }

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="plainPassword">明文密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>创建的用户实体</returns>
    public async Task<SysUser> CreateAsync(SysUser user, string plainPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);

        await EnsureUserNameUniqueAsync(user.UserName, null, user.TenantId, cancellationToken);

        var password = PasswordValueObject.Create(plainPassword, _passwordHasher);
        user.ChangePassword(password);
        user.MarkCreated();

        var created = await _userRepository.AddAsync(user, cancellationToken);
        await EnsureSecurityProfileAsync(created, cancellationToken);
        return created;
    }

    /// <summary>
    /// 修改用户密码
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="newPassword">新密码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    public async Task ChangePasswordAsync(SysUser user, string newPassword, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var password = PasswordValueObject.Create(newPassword, _passwordHasher);
        user.ChangePassword(password);
        await _userRepository.UpdateAsync(user, cancellationToken);

        var security = await _userSecurityRepository.GetByUserIdAsync(user.BasicId, user.TenantId, cancellationToken);
        if (security is null)
        {
            await EnsureSecurityProfileAsync(user, cancellationToken);
            return;
        }

        security.LastPasswordChangeTime = DateTimeOffset.UtcNow;
        security.PasswordExpiryTime = DateTimeOffset.UtcNow.AddDays(90);
        security.SecurityStamp = Guid.NewGuid().ToString("N");
        await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <summary>
    /// 校验用户名唯一性
    /// </summary>
    /// <param name="userName">用户名</param>
    /// <param name="excludeUserId">排除的用户ID</param>
    /// <param name="tenantId">租户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task EnsureUserNameUniqueAsync(
        string userName,
        long? excludeUserId = null,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        var exists = await _userRepository.IsUserNameExistsAsync(userName, excludeUserId, tenantId, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException($"用户名 '{userName}' 已存在");
        }
    }

    /// <summary>
    /// 确保用户安全配置存在
    /// </summary>
    /// <param name="user">用户实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns></returns>
    private async Task EnsureSecurityProfileAsync(SysUser user, CancellationToken cancellationToken)
    {
        var current = await _userSecurityRepository.GetByUserIdAsync(user.BasicId, user.TenantId, cancellationToken);
        if (current is not null)
        {
            return;
        }

        var security = new SysUserSecurity
        {
            TenantId = user.TenantId,
            UserId = user.BasicId,
            LastPasswordChangeTime = DateTimeOffset.UtcNow,
            PasswordExpiryTime = DateTimeOffset.UtcNow.AddDays(90),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsLocked = false
        };

        await _userSecurityRepository.AddAsync(security, cancellationToken);
    }
}
