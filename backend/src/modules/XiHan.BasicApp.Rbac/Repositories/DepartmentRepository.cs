#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentRepository
// Guid:c3d4e5f6-a7b8-4c5d-9e0f-1a2b3c4d5e6f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories.Abstracts;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Data.SqlSugar.Repository;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Rbac.Repositories;

/// <summary>
/// 部门仓储实现
/// </summary>
public class DepartmentRepository : SqlSugarAggregateRepository<SysDepartment, long>, IDepartmentRepository
{
    private readonly ISqlSugarClient _dbClient;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentRepository(ISqlSugarDbContext dbContext, IUnitOfWorkManager unitOfWorkManager)
        : base(dbContext, unitOfWorkManager)
    {
        _dbClient = dbContext.GetClient();
    }

    /// <summary>
    /// 根据部门编码查询部门
    /// </summary>
    public async Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysDepartment>()
            .FirstAsync(d => d.DepartmentCode == departmentCode, cancellationToken);
    }

    /// <summary>
    /// 检查部门编码是否存在
    /// </summary>
    public async Task<bool> ExistsByDepartmentCodeAsync(string departmentCode, long? excludeDepartmentId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbClient.Queryable<SysDepartment>()
            .Where(d => d.DepartmentCode == departmentCode);

        if (excludeDepartmentId.HasValue)
        {
            query = query.Where(d => d.BasicId != excludeDepartmentId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 获取子部门列表
    /// </summary>
    public async Task<List<SysDepartment>> GetChildrenAsync(long parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysDepartment>()
            .Where(d => d.ParentId == parentId)
            .OrderBy(d => d.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 递归获取所有子部门ID（包括孙部门）
    /// </summary>
    public async Task<List<long>> GetAllChildIdsAsync(long parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var allIds = new List<long>();
        var queue = new Queue<long>();
        queue.Enqueue(parentId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = await _dbClient.Queryable<SysDepartment>()
                .Where(d => d.ParentId == currentId)
                .Select(d => d.BasicId)
                .ToListAsync(cancellationToken);

            foreach (var childId in children)
            {
                allIds.Add(childId);
                queue.Enqueue(childId);
            }
        }

        return allIds;
    }

    /// <summary>
    /// 获取部门的父部门链（从根到当前部门）
    /// </summary>
    public async Task<List<SysDepartment>> GetParentChainAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var chain = new List<SysDepartment>();
        var currentId = departmentId;

        while (currentId != 0)
        {
            var department = await _dbClient.Queryable<SysDepartment>()
                .FirstAsync(d => d.BasicId == currentId, cancellationToken);

            if (department == null)
            {
                break;
            }

            chain.Insert(0, department);
            currentId = department.ParentId ?? 0;
        }

        return chain;
    }

    /// <summary>
    /// 获取租户下的所有部门
    /// </summary>
    public async Task<List<SysDepartment>> GetByTenantIdAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysDepartment>()
            //.Where(d => d.TenantId == tenantId)
            .OrderBy(d => d.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取部门的所有用户
    /// </summary>
    public async Task<List<SysUser>> GetUsersAsync(long departmentId, bool includeChildren = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!includeChildren)
        {
            return await _dbClient.Queryable<SysUser>()
                .LeftJoin<SysUserDepartment>((u, ud) => u.BasicId == ud.UserId)
                .Where((u, ud) => ud.DepartmentId == departmentId)
                .Select((u, ud) => u)
                .ToListAsync(cancellationToken);
        }

        // 获取所有子部门ID
        var departmentIds = await GetAllChildIdsAsync(departmentId, cancellationToken);
        departmentIds.Add(departmentId);

        return await _dbClient.Queryable<SysUser>()
            .LeftJoin<SysUserDepartment>((u, ud) => u.BasicId == ud.UserId)
            .Where((u, ud) => departmentIds.Contains(ud.DepartmentId))
            .Select((u, ud) => u)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 检查部门是否有子部门
    /// </summary>
    public async Task<bool> HasChildrenAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysDepartment>()
            .Where(d => d.ParentId == departmentId)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 检查部门是否有用户
    /// </summary>
    public async Task<bool> HasUsersAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysUserDepartment>()
            .Where(ud => ud.DepartmentId == departmentId)
            .AnyAsync(cancellationToken);
    }

    /// <summary>
    /// 检查设置父部门是否会形成循环引用
    /// </summary>
    public async Task<bool> WouldCreateCycleAsync(long departmentId, long parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentId = parentId;
        var visited = new HashSet<long> { departmentId };

        while (currentId != 0)
        {
            if (visited.Contains(currentId))
            {
                return true; // 检测到循环
            }

            visited.Add(currentId);

            var parent = await _dbClient.Queryable<SysDepartment>()
                .Where(d => d.BasicId == currentId)
                .FirstAsync(cancellationToken);

            if (parent == null || parent.ParentId == null)
            {
                break;
            }

            currentId = parent.ParentId.Value;
        }

        return false;
    }

    /// <summary>
    /// 根据父部门ID获取子部门列表（仅直接子部门）
    /// </summary>
    public async Task<List<SysDepartment>> GetByParentIdAsync(long parentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbClient.Queryable<SysDepartment>()
            .Where(d => d.ParentId == parentId)
            .OrderBy(d => d.Sort)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取部门的用户数量
    /// </summary>
    public async Task<int> GetDepartmentUserCountAsync(long departmentId, bool includeChildren = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!includeChildren)
        {
            return await _dbClient.Queryable<SysUserDepartment>()
                .Where(ud => ud.DepartmentId == departmentId)
                .CountAsync(cancellationToken);
        }

        // 获取所有子部门ID
        var departmentIds = await GetAllChildIdsAsync(departmentId, cancellationToken);
        departmentIds.Add(departmentId);

        return await _dbClient.Queryable<SysUserDepartment>()
            .Where(ud => departmentIds.Contains(ud.DepartmentId))
            .CountAsync(cancellationToken);
    }
}
