#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:FieldLevelSecurityQueryService
// Guid:8fda8f5c-0bb3-4b6e-907d-ff96b1eb4e4e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 字段级安全查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "字段级安全")]
public sealed class FieldLevelSecurityQueryService(
    IFieldLevelSecurityRepository fieldLevelSecurityRepository,
    IResourceRepository resourceRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    IDepartmentRepository departmentRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IFieldLevelSecurityQueryService
{
    /// <summary>
    /// 字段级安全仓储
    /// </summary>
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository = fieldLevelSecurityRepository;

    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository = resourceRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 获取字段级安全分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Read)]
    public async Task<PageResultDtoBase<FieldLevelSecurityListItemDto>> GetFieldLevelSecurityPageAsync(FieldLevelSecurityPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildFieldLevelSecurityPageRequest(input);
        var policies = await _fieldLevelSecurityRepository.GetPagedAsync(request, cancellationToken);
        if (policies.Items.Count == 0)
        {
            return new PageResultDtoBase<FieldLevelSecurityListItemDto>([], policies.Page);
        }

        var resourceMap = await BuildResourceMapAsync(policies.Items.Select(policy => policy.ResourceId), cancellationToken);
        var roleMap = await BuildRoleMapAsync(
            policies.Items
                .Where(policy => policy.TargetType == FieldSecurityTargetType.Role)
                .Select(policy => policy.TargetId),
            cancellationToken);
        var permissionMap = await BuildPermissionMapAsync(
            policies.Items
                .Where(policy => policy.TargetType == FieldSecurityTargetType.Permission)
                .Select(policy => policy.TargetId),
            cancellationToken);
        var departmentMap = await BuildDepartmentMapAsync(
            policies.Items
                .Where(policy => policy.TargetType == FieldSecurityTargetType.Department)
                .Select(policy => policy.TargetId),
            cancellationToken);
        var tenantMemberMap = await BuildTenantMemberMapAsync(
            policies.Items
                .Where(policy => policy.TargetType == FieldSecurityTargetType.User)
                .Select(policy => policy.TargetId),
            cancellationToken);

        var items = policies.Items
            .Select(policy =>
            {
                var (targetCode, targetName) = ResolveTargetSummary(policy, roleMap, permissionMap, departmentMap, tenantMemberMap);
                return FieldLevelSecurityApplicationMapper.ToListItemDto(
                    policy,
                    resourceMap.GetValueOrDefault(policy.ResourceId),
                    targetCode,
                    targetName);
            })
            .ToList();

        return new PageResultDtoBase<FieldLevelSecurityListItemDto>(items, policies.Page);
    }

    /// <summary>
    /// 获取字段级安全详情
    /// </summary>
    /// <param name="id">字段级安全主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>字段级安全详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.FieldLevelSecurity.Read)]
    public async Task<FieldLevelSecurityDetailDto?> GetFieldLevelSecurityDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "字段级安全主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var policy = await _fieldLevelSecurityRepository.GetByIdAsync(id, cancellationToken);
        if (policy is null)
        {
            return null;
        }

        var resource = await _resourceRepository.GetByIdAsync(policy.ResourceId, cancellationToken);
        var (targetCode, targetName) = await ResolveTargetSummaryAsync(policy, cancellationToken);

        return FieldLevelSecurityApplicationMapper.ToDetailDto(policy, resource, targetCode, targetName);
    }

    /// <summary>
    /// 构建字段级安全分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>字段级安全分页请求</returns>
    private static BasicAppPRDto BuildFieldLevelSecurityPageRequest(FieldLevelSecurityPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysFieldLevelSecurity.FieldName),
                nameof(SysFieldLevelSecurity.Description),
                nameof(SysFieldLevelSecurity.MaskPattern),
                nameof(SysFieldLevelSecurity.Remark));
        }

        if (input.TargetType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFieldLevelSecurity.TargetType), input.TargetType.Value);
        }

        if (input.TargetId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFieldLevelSecurity.TargetId), input.TargetId.Value);
        }

        if (input.ResourceId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFieldLevelSecurity.ResourceId), input.ResourceId.Value);
        }

        if (input.MaskStrategy.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFieldLevelSecurity.MaskStrategy), input.MaskStrategy.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysFieldLevelSecurity.Status), input.Status.Value);
        }

        request.Conditions.AddSort(nameof(SysFieldLevelSecurity.ResourceId), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysFieldLevelSecurity.TargetType), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysFieldLevelSecurity.Priority), SortDirection.Descending, 2);
        request.Conditions.AddSort(nameof(SysFieldLevelSecurity.FieldName), SortDirection.Ascending, 3);
        return request;
    }

    /// <summary>
    /// 解析目标摘要
    /// </summary>
    private static (string? Code, string? Name) ResolveTargetSummary(
        SysFieldLevelSecurity policy,
        IReadOnlyDictionary<long, SysRole> roleMap,
        IReadOnlyDictionary<long, SysPermission> permissionMap,
        IReadOnlyDictionary<long, SysDepartment> departmentMap,
        IReadOnlyDictionary<long, SysTenantUser> tenantMemberMap)
    {
        return policy.TargetType switch
        {
            FieldSecurityTargetType.Role => roleMap.TryGetValue(policy.TargetId, out var role)
                ? (role.RoleCode, role.RoleName)
                : (null, null),
            FieldSecurityTargetType.Permission => permissionMap.TryGetValue(policy.TargetId, out var permission)
                ? (permission.PermissionCode, permission.PermissionName)
                : (null, null),
            FieldSecurityTargetType.Department => departmentMap.TryGetValue(policy.TargetId, out var department)
                ? (department.DepartmentCode, department.DepartmentName)
                : (null, null),
            FieldSecurityTargetType.User => tenantMemberMap.TryGetValue(policy.TargetId, out var tenantMember)
                ? (null, tenantMember.DisplayName)
                : (null, null),
            _ => (null, null)
        };
    }

    /// <summary>
    /// 解析目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> ResolveTargetSummaryAsync(SysFieldLevelSecurity policy, CancellationToken cancellationToken)
    {
        return policy.TargetType switch
        {
            FieldSecurityTargetType.Role => await ResolveRoleTargetSummaryAsync(policy.TargetId, cancellationToken),
            FieldSecurityTargetType.Permission => await ResolvePermissionTargetSummaryAsync(policy.TargetId, cancellationToken),
            FieldSecurityTargetType.Department => await ResolveDepartmentTargetSummaryAsync(policy.TargetId, cancellationToken),
            FieldSecurityTargetType.User => await ResolveTenantMemberTargetSummaryAsync(policy.TargetId, cancellationToken),
            _ => (null, null)
        };
    }

    /// <summary>
    /// 解析角色目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> ResolveRoleTargetSummaryAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        return role is null ? (null, null) : (role.RoleCode, role.RoleName);
    }

    /// <summary>
    /// 解析权限目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> ResolvePermissionTargetSummaryAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
        return permission is null ? (null, null) : (permission.PermissionCode, permission.PermissionName);
    }

    /// <summary>
    /// 解析部门目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> ResolveDepartmentTargetSummaryAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
        return department is null ? (null, null) : (department.DepartmentCode, department.DepartmentName);
    }

    /// <summary>
    /// 解析租户成员目标摘要
    /// </summary>
    private async Task<(string? Code, string? Name)> ResolveTenantMemberTargetSummaryAsync(long userId, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken);
        return tenantMember is null ? (null, null) : (null, tenantMember.DisplayName);
    }

    /// <summary>
    /// 构建资源映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysResource>> BuildResourceMapAsync(IEnumerable<long> resourceIds, CancellationToken cancellationToken)
    {
        var ids = resourceIds
            .Where(resourceId => resourceId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysResource>();
        }

        var resources = await _resourceRepository.GetByIdsAsync(ids, cancellationToken);
        return resources.ToDictionary(resource => resource.BasicId);
    }

    /// <summary>
    /// 构建角色映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds
            .Where(roleId => roleId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(role => role.BasicId);
    }

    /// <summary>
    /// 构建权限映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysPermission>> BuildPermissionMapAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds
            .Where(permissionId => permissionId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysPermission>();
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        return permissions.ToDictionary(permission => permission.BasicId);
    }

    /// <summary>
    /// 构建部门映射
    /// </summary>
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

    /// <summary>
    /// 构建租户成员映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysTenantUser>> BuildTenantMemberMapAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(userId => userId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysTenantUser>();
        }

        var tenantMembers = await _tenantUserRepository.GetListAsync(
            tenantMember => ids.Contains(tenantMember.UserId),
            tenantMember => tenantMember.CreatedTime,
            cancellationToken);
        return tenantMembers.ToDictionary(tenantMember => tenantMember.UserId);
    }
}
