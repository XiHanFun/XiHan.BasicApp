using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 部门应用服务
/// </summary>
public interface IDepartmentAppService : IApplicationService
{
    /// <summary>
    /// 根据部门ID获取部门
    /// </summary>
    /// <param name="departmentId"></param>
    /// <returns></returns>
    Task<DepartmentDto?> GetByIdAsync(long departmentId);

    /// <summary>
    /// 获取子部门
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<IReadOnlyList<DepartmentDto>> GetChildrenAsync(long? parentId, long? tenantId = null);
}
