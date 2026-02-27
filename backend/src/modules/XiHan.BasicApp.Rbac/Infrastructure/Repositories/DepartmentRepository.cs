using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Infrastructure.Repositories;

/// <summary>
/// 部门仓储实现
/// </summary>
public class DepartmentRepository : SqlSugarAggregateRepository<SysDepartment, long>, IDepartmentRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="unitOfWorkManager"></param>
    public DepartmentRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    /// <param name="departmentCode"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);
        var query = _dbContext.CreateQueryable<SysDepartment>()
            .Where(department => department.DepartmentCode == departmentCode);

        if (tenantId.HasValue)
        {
            query = query.Where(department => department.TenantId == tenantId.Value);
        }
        else
        {
            query = query.Where(department => department.TenantId == null);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取子部门
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<SysDepartment>> GetChildrenAsync(long? parentId, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CreateQueryable<SysDepartment>()
            .Where(department => department.ParentId == parentId);

        if (tenantId.HasValue)
        {
            query = query.Where(department => department.TenantId == tenantId.Value);
        }

        return await query.OrderBy(department => department.Sort).ToListAsync(cancellationToken);
    }
}
