#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DataPermissionService
// Guid:f8a9bcde-f123-4567-890a-bcdef1234567
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/1/31 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Entities;
using XiHan.BasicApp.Rbac.Enums;
using XiHan.Framework.Data.SqlSugar;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Domain.Services.Implementations;

/// <summary>
/// 数据权限领域服务实现
/// </summary>
public class DataPermissionService : DomainService, IDataPermissionService
{
    private readonly ISysRoleRepository _roleRepository;
    private readonly ISysDepartmentRepository _departmentRepository;
    private readonly ISqlSugarDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DataPermissionService(
        ISysRoleRepository roleRepository,
        ISysDepartmentRepository departmentRepository,
        ISqlSugarDbContext dbContext)
    {
        _roleRepository = roleRepository;
        _departmentRepository = departmentRepository;
        _dbContext = dbContext;
    }

    /// <summary>
    /// 获取用户的数据权限范围
    /// </summary>
    public async Task<DataPermissionScope> GetUserDataScopeAsync(long userId, CancellationToken cancellationToken = default)
    {
        var roles = await _roleRepository.GetRolesByUserIdAsync(userId, cancellationToken);
        if (!roles.Any())
        {
            return DataPermissionScope.SelfOnly;
        }

        // 取最大权限范围
        var maxScope = roles.Max(r => r.DataScope);
        return maxScope;
    }

    /// <summary>
    /// 获取用户可访问的部门ID列表
    /// </summary>
    public async Task<List<long>> GetAccessibleDepartmentIdsAsync(long userId, CancellationToken cancellationToken = default)
    {
        var dataScope = await GetUserDataScopeAsync(userId, cancellationToken);

        switch (dataScope)
        {
            case DataPermissionScope.All:
                // 所有数据
                return await _dbContext.GetClient().Queryable<SysDepartment>()
                    .Where(d => d.Status == YesOrNo.Yes)
                    .Select(d => d.BasicId)
                    .ToListAsync(cancellationToken);

            case DataPermissionScope.DepartmentAndChildren:
                // 本部门及下级部门数据
                var departments = await _departmentRepository.GetDepartmentsByUserIdAsync(userId, cancellationToken);
                var departmentIds = new List<long>(departments.Select(d => d.BasicId));

                foreach (var dept in departments)
                {
                    var children = await GetDescendantDepartmentIdsAsync(dept.BasicId, cancellationToken);
                    departmentIds.AddRange(children);
                }

                return departmentIds.Distinct().ToList();

            case DataPermissionScope.DepartmentOnly:
                // 本部门数据
                var userDepartments = await _departmentRepository.GetDepartmentsByUserIdAsync(userId, cancellationToken);
                return userDepartments.Select(d => d.BasicId).ToList();

            case DataPermissionScope.SelfOnly:
                // 仅本人数据
                return [];

            case DataPermissionScope.Custom:
                // 自定义数据权限（通过角色数据权限配置）
                var roles = await _roleRepository.GetRolesByUserIdAsync(userId, cancellationToken);
                var customDeptIds = new List<long>();

                foreach (var role in roles)
                {
                    var deptIds = await GetRoleDataScopeDepartmentIdsAsync(role.BasicId, cancellationToken);
                    customDeptIds.AddRange(deptIds);
                }

                return customDeptIds.Distinct().ToList();

            default:
                return [];
        }
    }

    /// <summary>
    /// 检查用户是否可以访问指定部门的数据
    /// </summary>
    public async Task<bool> CanAccessDepartmentDataAsync(long userId, long departmentId, CancellationToken cancellationToken = default)
    {
        var accessibleDepartmentIds = await GetAccessibleDepartmentIdsAsync(userId, cancellationToken);
        return accessibleDepartmentIds.Contains(departmentId);
    }

    /// <summary>
    /// 检查用户是否可以访问指定用户的数据
    /// </summary>
    public async Task<bool> CanAccessUserDataAsync(long currentUserId, long targetUserId, CancellationToken cancellationToken = default)
    {
        // 自己的数据总是可以访问
        if (currentUserId == targetUserId)
        {
            return true;
        }

        var dataScope = await GetUserDataScopeAsync(currentUserId, cancellationToken);

        // 全部数据权限
        if (dataScope == DataPermissionScope.All)
        {
            return true;
        }

        // 仅本人数据
        if (dataScope == DataPermissionScope.SelfOnly)
        {
            return false;
        }

        // 获取目标用户的部门
        var targetUserDepartments = await _departmentRepository.GetDepartmentsByUserIdAsync(targetUserId, cancellationToken);
        var targetDepartmentIds = targetUserDepartments.Select(d => d.BasicId).ToList();

        // 获取当前用户可访问的部门
        var accessibleDepartmentIds = await GetAccessibleDepartmentIdsAsync(currentUserId, cancellationToken);

        // 检查是否有交集
        return targetDepartmentIds.Any(id => accessibleDepartmentIds.Contains(id));
    }

    /// <summary>
    /// 根据角色获取数据权限范围
    /// </summary>
    public async Task<DataPermissionScope> GetRoleDataScopeAsync(long roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        return role?.DataScope ?? DataPermissionScope.SelfOnly;
    }

    /// <summary>
    /// 获取角色的数据权限部门列表
    /// </summary>
    public async Task<List<long>> GetRoleDataScopeDepartmentIdsAsync(long roleId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GetClient().Queryable<SysRoleDataScope>()
            .Where(rds => rds.RoleId == roleId)
            .Select(rds => rds.DepartmentId)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 递归获取部门的所有子部门ID
    /// </summary>
    private async Task<List<long>> GetDescendantDepartmentIdsAsync(long departmentId, CancellationToken cancellationToken)
    {
        var result = new List<long>();
        var children = await _departmentRepository.GetChildrenDepartmentsAsync(departmentId, cancellationToken);

        foreach (var child in children)
        {
            result.Add(child.BasicId);
            var descendants = await GetDescendantDepartmentIdsAsync(child.BasicId, cancellationToken);
            result.AddRange(descendants);
        }

        return result;
    }
}
