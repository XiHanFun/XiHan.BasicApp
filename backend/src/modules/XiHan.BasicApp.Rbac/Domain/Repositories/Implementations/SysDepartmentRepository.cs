#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysDepartmentRepository
// Guid:c9d0e1f2-a3b4-5678-9abc-def123456789
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Domain.Repositories.Implementations;

/// <summary>
/// 系统部门仓储实现
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
    public async Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.DepartmentCode == departmentCode)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 检查部门编码是否存在
    /// </summary>
    public async Task<bool> IsDepartmentCodeExistsAsync(string departmentCode, long? excludeDepartmentId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.DepartmentCode == departmentCode);

        if (excludeDepartmentId.HasValue)
        {
            query = query.Where(d => d.BasicId != excludeDepartmentId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取用户所属的部门列表
    /// </summary>
    public async Task<List<SysDepartment>> GetDepartmentsByUserIdAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDepartment>()
            .InnerJoin<SysUserDepartment>((d, ud) => d.BasicId == ud.DepartmentId)
            .Where((d, ud) => ud.UserId == userId)
            .Where((d, ud) => d.Status == YesOrNo.Yes)
            .Select((d, ud) => d)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取父级部门下的子部门列表
    /// </summary>
    public async Task<List<SysDepartment>> GetChildrenDepartmentsAsync(long? parentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.ParentId == parentId)
            .Where(d => d.Status == YesOrNo.Yes)
            .OrderBy(d => d.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取租户下的部门树
    /// </summary>
    public async Task<List<SysDepartment>> GetDepartmentTreeByTenantAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysDepartment>()
            .Where(d => d.TenantId == tenantId)
            .Where(d => d.Status == YesOrNo.Yes)
            .OrderBy(d => d.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取部门的所有祖先部门
    /// </summary>
    public async Task<List<SysDepartment>> GetAncestorDepartmentsAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        var result = new List<SysDepartment>();
        var currentId = departmentId;

        while (currentId > 0)
        {
            var department = await GetByIdAsync(currentId, cancellationToken);
            if (department == null || !department.ParentId.HasValue)
            {
                break;
            }

            var parent = await GetByIdAsync(department.ParentId.Value, cancellationToken);
            if (parent != null)
            {
                result.Add(parent);
                currentId = parent.BasicId;
            }
            else
            {
                break;
            }
        }

        result.Reverse();
        return result;
    }

    /// <summary>
    /// 保存部门
    /// </summary>
    public async Task<SysDepartment> SaveAsync(SysDepartment department, CancellationToken cancellationToken = default)
    {
        if (department.IsTransient())
        {
            return await AddAsync(department, cancellationToken);
        }
        else
        {
            return await UpdateAsync(department, cancellationToken);
        }
    }

    /// <summary>
    /// 启用部门
    /// </summary>
    public async Task EnableDepartmentAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysDepartment>()
            .SetColumns(d => d.Status == YesOrNo.Yes)
            .Where(d => d.BasicId == departmentId)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 禁用部门
    /// </summary>
    public async Task DisableDepartmentAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Updateable<SysDepartment>()
            .SetColumns(d => d.Status == YesOrNo.No)
            .Where(d => d.BasicId == departmentId)
            .ExecuteCommandAsync(cancellationToken);
    }
}
