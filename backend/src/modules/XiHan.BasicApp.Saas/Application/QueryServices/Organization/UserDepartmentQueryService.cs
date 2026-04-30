#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDepartmentQueryService
// Guid:ebf37f4c-bf38-4d7e-840f-8e83d450eb17
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
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户部门归属查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户部门")]
public sealed class UserDepartmentQueryService(
    IUserDepartmentRepository userDepartmentRepository,
    IDepartmentRepository departmentRepository,
    IDepartmentHierarchyRepository departmentHierarchyRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserDepartmentQueryService
{
    /// <summary>
    /// 用户部门仓储
    /// </summary>
    private readonly IUserDepartmentRepository _userDepartmentRepository = userDepartmentRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 部门层级仓储
    /// </summary>
    private readonly IDepartmentHierarchyRepository _departmentHierarchyRepository = departmentHierarchyRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取用户部门归属列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回有效归属</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Read)]
    public async Task<IReadOnlyList<UserDepartmentListItemDto>> GetUserDepartmentsAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _ = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        var userDepartments = onlyValid
            ? await _userDepartmentRepository.GetValidByUserIdAsync(userId, cancellationToken)
            : await _userDepartmentRepository.GetListAsync(
                userDepartment => userDepartment.UserId == userId,
                userDepartment => userDepartment.CreatedTime,
                cancellationToken);

        return await MapListAsync(userDepartments, cancellationToken);
    }

    /// <summary>
    /// 获取部门用户归属列表
    /// </summary>
    /// <param name="departmentId">部门主键</param>
    /// <param name="includeChildren">是否包含子部门</param>
    /// <param name="onlyValid">是否仅返回有效归属</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门用户归属列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Read)]
    public async Task<IReadOnlyList<UserDepartmentListItemDto>> GetDepartmentUsersAsync(long departmentId, bool includeChildren = false, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId), "部门主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _ = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        var departmentIds = await ResolveDepartmentIdsAsync(departmentId, includeChildren, cancellationToken);
        var userDepartments = onlyValid
            ? await _userDepartmentRepository.GetListAsync(
                userDepartment => departmentIds.Contains(userDepartment.DepartmentId) && userDepartment.Status == ValidityStatus.Valid,
                userDepartment => userDepartment.CreatedTime,
                cancellationToken)
            : await _userDepartmentRepository.GetListAsync(
                userDepartment => departmentIds.Contains(userDepartment.DepartmentId),
                userDepartment => userDepartment.CreatedTime,
                cancellationToken);

        return await MapListAsync(userDepartments, cancellationToken);
    }

    /// <summary>
    /// 获取用户部门归属详情
    /// </summary>
    /// <param name="id">用户部门归属主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Read)]
    public async Task<UserDepartmentDetailDto?> GetUserDepartmentDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户部门归属主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var userDepartment = await _userDepartmentRepository.GetByIdAsync(id, cancellationToken);
        if (userDepartment is null)
        {
            return null;
        }

        var department = await _departmentRepository.GetByIdAsync(userDepartment.DepartmentId, cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(userDepartment, department);
    }

    /// <summary>
    /// 解析部门筛选范围
    /// </summary>
    private async Task<IReadOnlyList<long>> ResolveDepartmentIdsAsync(long departmentId, bool includeChildren, CancellationToken cancellationToken)
    {
        if (!includeChildren)
        {
            return [departmentId];
        }

        var descendantIds = await _departmentHierarchyRepository.GetDescendantIdsAsync(departmentId, includeSelf: true, cancellationToken);
        return descendantIds.Count == 0 ? [departmentId] : descendantIds;
    }

    /// <summary>
    /// 映射用户部门归属列表
    /// </summary>
    private async Task<IReadOnlyList<UserDepartmentListItemDto>> MapListAsync(IReadOnlyList<SysUserDepartment> userDepartments, CancellationToken cancellationToken)
    {
        if (userDepartments.Count == 0)
        {
            return [];
        }

        var departmentMap = await BuildDepartmentMapAsync(userDepartments.Select(userDepartment => userDepartment.DepartmentId), cancellationToken);

        return [.. userDepartments
            .Select(userDepartment => UserDepartmentApplicationMapper.ToListItemDto(
                userDepartment,
                departmentMap.GetValueOrDefault(userDepartment.DepartmentId)))
            .OrderByDescending(item => item.IsMain)
            .ThenBy(item => item.DepartmentCode)
            .ThenBy(item => item.DepartmentId)
            .ThenBy(item => item.UserId)];
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
