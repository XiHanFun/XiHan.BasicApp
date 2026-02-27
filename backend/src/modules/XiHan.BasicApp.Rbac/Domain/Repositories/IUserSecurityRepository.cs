using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户安全状态仓储接口
/// </summary>
public interface IUserSecurityRepository : IRepositoryBase<SysUserSecurity, long>
{
    /// <summary>
    /// 根据用户 ID 获取安全状态
    /// </summary>
    Task<SysUserSecurity?> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
