using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户聚合仓储接口
/// </summary>
public interface IUserRepository : IAggregateRootRepository<SysUser, long>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    Task<SysUser?> GetByUserNameAsync(string userName, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 校验用户名是否已存在
    /// </summary>
    Task<bool> IsUserNameExistsAsync(string userName, long? excludeUserId = null, long? tenantId = null, CancellationToken cancellationToken = default);
}
