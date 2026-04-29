#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RoleDataScopeQueryService
// Guid:a00f87fb-9d85-4ef8-916e-4920fd729f4b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 角色数据范围查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "角色数据范围")]
public sealed class RoleDataScopeQueryService(
    IRoleRepository roleRepository,
    IRoleDataScopeRepository roleDataScopeRepository,
    IDepartmentRepository departmentRepository)
    : SaasApplicationService, IRoleDataScopeQueryService
{
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 角色数据范围仓储
    /// </summary>
    private readonly IRoleDataScopeRepository _roleDataScopeRepository = roleDataScopeRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 获取角色数据范围列表
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="onlyValid">是否仅返回当前有效数据范围</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Read)]
    public async Task<IReadOnlyList<RoleDataScopeListItemDto>> GetRoleDataScopesAsync(long roleId, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "角色主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _ = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        var scopes = onlyValid
            ? await _roleDataScopeRepository.GetValidByRoleIdsAsync([roleId], DateTimeOffset.UtcNow, cancellationToken)
            : await _roleDataScopeRepository.GetListAsync(
                scope => scope.RoleId == roleId,
                scope => scope.CreatedTime,
                cancellationToken);

        if (scopes.Count == 0)
        {
            return [];
        }

        var departmentMap = await BuildDepartmentMapAsync(scopes.Select(scope => scope.DepartmentId), cancellationToken);

        return [.. scopes
            .Select(scope => RoleDataScopeApplicationMapper.ToListItemDto(
                scope,
                departmentMap.GetValueOrDefault(scope.DepartmentId)))
            .OrderBy(item => item.DepartmentCode)
            .ThenBy(item => item.DepartmentId)];
    }

    /// <summary>
    /// 获取角色数据范围详情
    /// </summary>
    /// <param name="id">角色数据范围主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色数据范围详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.RoleDataScope.Read)]
    public async Task<RoleDataScopeDetailDto?> GetRoleDataScopeDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "角色数据范围主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var scope = await _roleDataScopeRepository.GetByIdAsync(id, cancellationToken);
        if (scope is null)
        {
            return null;
        }

        var department = await _departmentRepository.GetByIdAsync(scope.DepartmentId, cancellationToken);
        return RoleDataScopeApplicationMapper.ToDetailDto(scope, department);
    }

    /// <summary>
    /// 构建部门映射
    /// </summary>
    /// <param name="departmentIds">部门主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门映射</returns>
    private async Task<IReadOnlyDictionary<long, SysDepartment>> BuildDepartmentMapAsync(IEnumerable<long> departmentIds, CancellationToken cancellationToken)
    {
        var ids = departmentIds
            .Where(departmentId => departmentId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysDepartment>();
        }

        var departments = await _departmentRepository.GetByIdsAsync(ids, cancellationToken);
        return departments.ToDictionary(department => department.BasicId);
    }
}
