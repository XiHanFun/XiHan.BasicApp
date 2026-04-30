#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDataScopeQueryService
// Guid:8d7b0276-df33-4f17-bbe3-2e6bd905ba52
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
/// 用户数据范围查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户数据范围")]
public sealed class UserDataScopeQueryService(
    IUserDataScopeRepository userDataScopeRepository,
    IDepartmentRepository departmentRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserDataScopeQueryService
{
    /// <summary>
    /// 用户数据范围仓储
    /// </summary>
    private readonly IUserDataScopeRepository _userDataScopeRepository = userDataScopeRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取用户数据范围列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回当前有效数据范围覆盖</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Read)]
    public async Task<IReadOnlyList<UserDataScopeListItemDto>> GetUserDataScopesAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        var scopes = onlyValid
            ? await _userDataScopeRepository.GetValidByUserIdAsync(userId, cancellationToken)
            : await _userDataScopeRepository.GetListAsync(
                scope => scope.UserId == userId,
                scope => scope.CreatedTime,
                cancellationToken);

        if (scopes.Count == 0)
        {
            return [];
        }

        var departmentMap = await BuildDepartmentMapAsync(scopes.Select(scope => scope.DepartmentId), cancellationToken);

        return [.. scopes
            .Select(scope => UserDataScopeApplicationMapper.ToListItemDto(
                scope,
                departmentMap.GetValueOrDefault(scope.DepartmentId),
                tenantMember))
            .OrderBy(item => item.DepartmentCode)
            .ThenBy(item => item.DepartmentId)];
    }

    /// <summary>
    /// 获取用户数据范围详情
    /// </summary>
    /// <param name="id">用户数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Read)]
    public async Task<UserDataScopeDetailDto?> GetUserDataScopeDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户数据范围绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var scope = await _userDataScopeRepository.GetByIdAsync(id, cancellationToken);
        if (scope is null)
        {
            return null;
        }

        var department = scope.DepartmentId > 0
            ? await _departmentRepository.GetByIdAsync(scope.DepartmentId, cancellationToken)
            : null;
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(scope.UserId, cancellationToken);

        return UserDataScopeApplicationMapper.ToDetailDto(scope, department, tenantMember);
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
