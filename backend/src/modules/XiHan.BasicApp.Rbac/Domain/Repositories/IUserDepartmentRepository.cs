using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户部门关系仓储接口
/// </summary>
public interface IUserDepartmentRepository : IRepositoryBase<SysUserDepartment, long>
{
    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    Task<IReadOnlyList<SysUserDepartment>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户部门关系
    /// </summary>
    Task<bool> RemoveByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
