using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 部门聚合仓储接口
/// </summary>
public interface IDepartmentRepository : IAggregateRootRepository<SysDepartment, long>
{
    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取下级部门
    /// </summary>
    Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long? parentId, long? tenantId = null, CancellationToken cancellationToken = default);
}
