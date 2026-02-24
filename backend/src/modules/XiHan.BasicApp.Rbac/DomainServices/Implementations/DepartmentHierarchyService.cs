#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentHierarchyService
// Guid:0cda05ea-a6ca-4bed-b586-66d48da724a6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/01/31 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.DomainServices;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Repositories;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.DomainServices.Implementations;

/// <summary>
/// 部门层级领域服务实现
/// </summary>
public class DepartmentHierarchyService : DomainService, IDepartmentHierarchyService
{
    private readonly ISysDepartmentRepository _departmentRepository;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentHierarchyService(ISysDepartmentRepository departmentRepository, ISqlSugarDbContext dbContext)
    {
        _departmentRepository = departmentRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 构建部门树
    /// </summary>
    public async Task<List<SysDepartment>> BuildDepartmentTreeAsync(long tenantId, CancellationToken cancellationToken = default)
    {
        var allDepartments = await _departmentRepository.GetDepartmentTreeByTenantAsync(tenantId, cancellationToken);
        return BuildTree(allDepartments, null);
    }

    /// <summary>
    /// 获取部门的所有子部门（递归）
    /// </summary>
    public async Task<List<SysDepartment>> GetDescendantDepartmentsAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        var result = new List<SysDepartment>();
        await CollectDescendantDepartmentsAsync(departmentId, result, cancellationToken);
        return result;
    }

    /// <summary>
    /// 获取部门的所有祖先部门
    /// </summary>
    public async Task<List<SysDepartment>> GetAncestorDepartmentsAsync(long departmentId, CancellationToken cancellationToken = default)
    {
        return await _departmentRepository.GetAncestorDepartmentsAsync(departmentId, cancellationToken);
    }

    /// <summary>
    /// 检查部门是否为另一个部门的子部门
    /// </summary>
    public async Task<bool> IsDescendantOfAsync(long departmentId, long ancestorId, CancellationToken cancellationToken = default)
    {
        var ancestors = await GetAncestorDepartmentsAsync(departmentId, cancellationToken);
        return ancestors.Any(d => d.BasicId == ancestorId);
    }

    /// <summary>
    /// 验证部门层级关系是否有效
    /// </summary>
    public async Task<bool> ValidateDepartmentHierarchyAsync(long departmentId, long? parentId, CancellationToken cancellationToken = default)
    {
        if (!parentId.HasValue)
        {
            return true;
        }

        // 不能将部门设置为自己的父部门
        if (departmentId == parentId.Value)
        {
            return false;
        }

        // 检查是否会形成循环引用
        var isDescendant = await IsDescendantOfAsync(parentId.Value, departmentId, cancellationToken);
        if (isDescendant)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取部门路径字符串
    /// </summary>
    public async Task<string> GetDepartmentPathAsync(long departmentId, string separator = "/", CancellationToken cancellationToken = default)
    {
        var breadcrumb = new List<string>();
        var currentDept = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);

        while (currentDept != null)
        {
            breadcrumb.Insert(0, currentDept.DepartmentName);

            if (!currentDept.ParentId.HasValue)
            {
                break;
            }

            currentDept = await _departmentRepository.GetByIdAsync(currentDept.ParentId.Value, cancellationToken);
        }

        return string.Join(separator, breadcrumb);
    }

    /// <summary>
    /// 批量为用户分配部门
    /// </summary>
    public async Task AssignDepartmentsToUserAsync(long userId, IEnumerable<long> departmentIds, CancellationToken cancellationToken = default)
    {
        var userDepartments = departmentIds.Select(deptId => new SysUserDepartment
        {
            UserId = userId,
            DepartmentId = deptId,
            CreatedTime = DateTimeOffset.Now
        }).ToList();

        await _dbContext.GetClient().Insertable(userDepartments)
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 批量移除用户的部门
    /// </summary>
    public async Task RemoveDepartmentsFromUserAsync(long userId, IEnumerable<long> departmentIds, CancellationToken cancellationToken = default)
    {
        await _dbContext.GetClient().Deleteable<SysUserDepartment>()
            .Where(ud => ud.UserId == userId && departmentIds.Contains(ud.DepartmentId))
            .ExecuteCommandAsync(cancellationToken);
    }

    /// <summary>
    /// 构建部门树
    /// </summary>
    private List<SysDepartment> BuildTree(List<SysDepartment> allDepartments, long? parentId)
    {
        return allDepartments
            .Where(d => d.ParentId == parentId)
            .OrderBy(d => d.Sort)
            .ToList();
    }

    /// <summary>
    /// 递归收集所有子部门
    /// </summary>
    private async Task CollectDescendantDepartmentsAsync(long departmentId, List<SysDepartment> result, CancellationToken cancellationToken)
    {
        var children = await _departmentRepository.GetChildrenDepartmentsAsync(departmentId, cancellationToken);

        foreach (var child in children)
        {
            result.Add(child);
            await CollectDescendantDepartmentsAsync(child.BasicId, result, cancellationToken);
        }
    }
}
