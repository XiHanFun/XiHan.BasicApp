#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentRepository
// Guid:856687ef-ac26-4c4a-9191-bf1caa7171f1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:53:35
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Repository;

using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 部门仓储实现
/// </summary>
public class DepartmentRepository : SqlSugarAggregateRepository<SysDepartment, long>, IDepartmentRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="clientResolver"></param>
    /// <param name="unitOfWorkManager"></param>
    public DepartmentRepository(
        ISqlSugarClientResolver clientResolver,
        IUnitOfWorkManager unitOfWorkManager)
        : base(clientResolver, unitOfWorkManager)
    {
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
        var query = CreateQueryable()
            .Where(department => department.DepartmentCode == departmentCode);

        query = tenantId.HasValue
            ? query.Where(department => department.TenantId == tenantId.Value)
            : query.Where(department => department.TenantId == 0);

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
        var query = CreateQueryable()
            .Where(department => department.ParentId == parentId);

        if (tenantId.HasValue)
        {
            query = query.Where(department => department.TenantId == tenantId.Value);
        }

        return await query.OrderBy(department => department.Sort).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 获取后代部门ID
    /// </summary>
    /// <param name="departmentId"></param>
    /// <param name="includeSelf"></param>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetDescendantIdsAsync(
        long departmentId,
        bool includeSelf = true,
        long? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        if (departmentId <= 0)
        {
            return [];
        }

        var resolvedTenantId = tenantId;
        var query = CreateQueryable<SysDepartmentHierarchy>()
            .Where(hierarchy => hierarchy.AncestorId == departmentId);

        if (!includeSelf)
        {
            query = query.Where(hierarchy => hierarchy.Depth > 0);
        }

        query = resolvedTenantId.HasValue
            ? query.Where(hierarchy => hierarchy.TenantId == resolvedTenantId.Value)
            : query.Where(hierarchy => hierarchy.TenantId == 0);

        var ids = await query
            .Select(hierarchy => hierarchy.DescendantId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return ids;
    }

    /// <summary>
    /// 重建部门层级闭包表
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task RebuildHierarchyAsync(long? tenantId = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolvedTenantId = tenantId;
        var departmentQuery = CreateQueryable();
        departmentQuery = resolvedTenantId.HasValue
            ? departmentQuery.Where(department => department.TenantId == resolvedTenantId.Value)
            : departmentQuery.Where(department => department.TenantId == 0);

        var departments = await departmentQuery.ToListAsync(cancellationToken);
        var departmentMap = departments.ToDictionary(department => department.BasicId);

        var deleteable = DbClient.Deleteable<SysDepartmentHierarchy>();
        deleteable = resolvedTenantId.HasValue
            ? deleteable.Where(hierarchy => hierarchy.TenantId == resolvedTenantId.Value)
            : deleteable.Where(hierarchy => hierarchy.TenantId == 0);

        await deleteable.ExecuteCommandAsync(cancellationToken);
        if (departments.Count == 0)
        {
            return;
        }

        var hierarchyRows = new List<SysDepartmentHierarchy>(departments.Count * 2);
        foreach (var department in departments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chain = BuildAncestorChain(department, departmentMap);
            for (var index = 0; index < chain.Count; index++)
            {
                var ancestor = chain[index];
                var pathDepartments = chain.Skip(index).ToArray();

                var row = new SysDepartmentHierarchy
                {
                    TenantId = resolvedTenantId ?? 0,
                    AncestorId = ancestor.BasicId,
                    DescendantId = department.BasicId,
                    Depth = pathDepartments.Length - 1,
                    Path = string.Join("/", pathDepartments.Select(item => item.BasicId)),
                    PathName = string.Join("/", pathDepartments.Select(item => item.DepartmentName))
                };

                hierarchyRows.Add(row);
            }
        }

        if (hierarchyRows.Count == 0)
        {
            return;
        }

        await DbClient.Insertable(hierarchyRows).ExecuteCommandAsync(cancellationToken);
    }

    private static IReadOnlyList<SysDepartment> BuildAncestorChain(
        SysDepartment department,
        IReadOnlyDictionary<long, SysDepartment> departmentMap)
    {
        var stack = new Stack<SysDepartment>();
        var visited = new HashSet<long>();

        var current = department;
        while (visited.Add(current.BasicId))
        {
            stack.Push(current);

            if (!current.ParentId.HasValue || current.ParentId.Value <= 0)
            {
                break;
            }

            if (!departmentMap.TryGetValue(current.ParentId.Value, out var parent))
            {
                break;
            }

            current = parent;
        }

        return stack.ToArray();
    }
}
