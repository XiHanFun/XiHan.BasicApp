#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentRepository
// Guid:e3f4a5b6-c7d8-9012-3456-789012e78901
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/30 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 部门聚合仓储实现
/// </summary>
public class SysDepartmentRepository : SqlSugarAggregateRepository<SysDepartment, long>, ISysDepartmentRepository
{
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SysDepartmentRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    public async Task<SysDepartment?> GetByDeptCodeAsync(string deptCode, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.DepartmentCode == deptCode && d.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId.Value);
        }

        return await query.FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户所属部门
    /// </summary>
    public async Task<List<SysDepartment>> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDepartment>()
            .InnerJoin<SysUserDepartment>((d, ud) => d.BasicId == ud.DepartmentId)
            .Where((d, ud) => ud.UserId == userId && d.Status == YesOrNo.Yes && ud.Status == YesOrNo.Yes)
            .Select(d => d)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取子部门
    /// </summary>
    public async Task<List<SysDepartment>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.ParentId == parentId && d.Status == YesOrNo.Yes)
            .OrderBy(d => d.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取部门树
    /// </summary>
    public async Task<List<SysDepartment>> GetDepartmentTreeAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.Status == YesOrNo.Yes);

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId.Value);
        }

        return await query.OrderBy(d => d.Sort).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 批量获取部门
    /// </summary>
    public async Task<List<SysDepartment>> GetByIdsAsync(IEnumerable<long> deptIds, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDepartment>()
            .In(d => d.BasicId, deptIds.ToArray())
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查部门编码是否存在
    /// </summary>
    public async Task<bool> ExistsByDeptCodeAsync(string deptCode, long? excludeDeptId = null, long? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.DepartmentCode == deptCode);

        if (excludeDeptId.HasValue)
        {
            query = query.Where(d => d.BasicId != excludeDeptId.Value);
        }

        if (tenantId.HasValue)
        {
            query = query.Where(d => d.TenantId == tenantId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
