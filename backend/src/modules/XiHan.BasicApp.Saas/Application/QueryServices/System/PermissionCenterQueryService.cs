#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionCenterQueryService
// Guid:ce1bdd4d-1afc-462c-a452-74d9ce60b727
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using SqlSugar;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限中心页面查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限中心")]
public sealed class PermissionCenterQueryService(
    IPermissionRepository permissionRepository,
    IResourceRepository resourceRepository,
    IOperationRepository operationRepository,
    IRolePermissionRepository rolePermissionRepository,
    IUserPermissionRepository userPermissionRepository,
    IPermissionConditionRepository permissionConditionRepository,
    IPermissionDelegationRepository permissionDelegationRepository,
    IPermissionRequestRepository permissionRequestRepository,
    IFieldLevelSecurityRepository fieldLevelSecurityRepository,
    IRoleRepository roleRepository,
    IDepartmentRepository departmentRepository,
    ITenantUserRepository tenantUserRepository,
    IReviewRepository reviewRepository,
    ISqlSugarClientResolver clientResolver)
    : SaasApplicationService, IPermissionCenterQueryService
{
    private const int MaxDelegationCount = 50;
    private const int MaxRequestCount = 50;
    private const int MaxFieldSecurityCount = 100;
    private const int MaxChangeLogCount = 20;

    private readonly IPermissionRepository _permissionRepository = permissionRepository;
    private readonly IResourceRepository _resourceRepository = resourceRepository;
    private readonly IOperationRepository _operationRepository = operationRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository = rolePermissionRepository;
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;
    private readonly IPermissionConditionRepository _permissionConditionRepository = permissionConditionRepository;
    private readonly IPermissionDelegationRepository _permissionDelegationRepository = permissionDelegationRepository;
    private readonly IPermissionRequestRepository _permissionRequestRepository = permissionRequestRepository;
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
    private readonly IRoleRepository _roleRepository = roleRepository;
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;
    private readonly IReviewRepository _reviewRepository = reviewRepository;

    private ISqlSugarClient DbClient => clientResolver.GetCurrentClient();

    /// <inheritdoc />
    [PermissionAuthorize(SaasPermissionCodes.Permission.Read)]
    public async Task<PermissionCenterDetailDto?> GetPermissionCenterDetailAsync(long permissionId, CancellationToken cancellationToken = default)
    {
        if (permissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(permissionId), "权限主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken);
        if (permission is null)
        {
            return null;
        }

        var resource = permission.ResourceId.HasValue
            ? await _resourceRepository.GetByIdAsync(permission.ResourceId.Value, cancellationToken)
            : null;
        var operation = permission.OperationId.HasValue
            ? await _operationRepository.GetByIdAsync(permission.OperationId.Value, cancellationToken)
            : null;
        var now = DateTimeOffset.UtcNow;

        return new PermissionCenterDetailDto
        {
            Permission = PermissionApplicationMapper.ToDetailDto(permission, resource, operation),
            Resource = resource is null ? null : ResourceApplicationMapper.ToDetailDto(resource),
            Operation = operation is null ? null : OperationApplicationMapper.ToDetailDto(operation),
            Conditions = await GetConditionsAsync(permission, cancellationToken),
            Delegations = await GetDelegationsAsync(permission, now, cancellationToken),
            Requests = await GetRequestsAsync(permission, now, cancellationToken),
            FieldSecurities = await GetFieldSecuritiesAsync(permission, cancellationToken),
            ChangeLogs = await GetChangeLogsAsync(permission.BasicId, cancellationToken),
            GeneratedTime = now
        };
    }

    private async Task<List<PermissionConditionListItemDto>> GetConditionsAsync(SysPermission permission, CancellationToken cancellationToken)
    {
        var rolePermissions = await _rolePermissionRepository.GetListAsync(
            item => item.PermissionId == permission.BasicId,
            item => item.CreatedTime,
            cancellationToken);
        var userPermissions = await _userPermissionRepository.GetListAsync(
            item => item.PermissionId == permission.BasicId,
            item => item.CreatedTime,
            cancellationToken);

        if (rolePermissions.Count == 0 && userPermissions.Count == 0)
        {
            return [];
        }

        var rolePermissionIds = rolePermissions.Select(item => item.BasicId).ToArray();
        var userPermissionIds = userPermissions.Select(item => item.BasicId).ToArray();
        var conditions = await GetConditionsByAuthorizationIdsAsync(rolePermissionIds, userPermissionIds, cancellationToken);
        if (conditions.Count == 0)
        {
            return [];
        }

        var rolePermissionMap = rolePermissions.ToDictionary(item => item.BasicId);
        var userPermissionMap = userPermissions.ToDictionary(item => item.BasicId);
        var roleMap = await BuildRoleMapAsync(rolePermissions.Select(item => item.RoleId), cancellationToken);
        var tenantMemberMap = await BuildTenantMemberMapAsync(userPermissions.Select(item => item.UserId), cancellationToken);

        return [.. conditions
            .Select(condition =>
            {
                var rolePermission = condition.RolePermissionId.HasValue
                    ? rolePermissionMap.GetValueOrDefault(condition.RolePermissionId.Value)
                    : null;
                var userPermission = condition.UserPermissionId.HasValue
                    ? userPermissionMap.GetValueOrDefault(condition.UserPermissionId.Value)
                    : null;

                return PermissionConditionApplicationMapper.ToListItemDto(
                    condition,
                    rolePermission,
                    userPermission,
                    permission,
                    rolePermission is null ? null : roleMap.GetValueOrDefault(rolePermission.RoleId),
                    userPermission is null ? null : tenantMemberMap.GetValueOrDefault(userPermission.UserId));
            })
            .OrderBy(item => item.ConditionGroup)
            .ThenBy(item => item.AttributeName)
            .ThenBy(item => item.BasicId)];
    }

    private async Task<IReadOnlyList<SysPermissionCondition>> GetConditionsByAuthorizationIdsAsync(
        long[] rolePermissionIds,
        long[] userPermissionIds,
        CancellationToken cancellationToken)
    {
        if (rolePermissionIds.Length > 0 && userPermissionIds.Length > 0)
        {
            return await _permissionConditionRepository.GetListAsync(
                item => (item.RolePermissionId.HasValue && rolePermissionIds.Contains(item.RolePermissionId.Value))
                    || (item.UserPermissionId.HasValue && userPermissionIds.Contains(item.UserPermissionId.Value)),
                item => item.ConditionGroup,
                cancellationToken);
        }

        if (rolePermissionIds.Length > 0)
        {
            return await _permissionConditionRepository.GetListAsync(
                item => item.RolePermissionId.HasValue && rolePermissionIds.Contains(item.RolePermissionId.Value),
                item => item.ConditionGroup,
                cancellationToken);
        }

        if (userPermissionIds.Length > 0)
        {
            return await _permissionConditionRepository.GetListAsync(
                item => item.UserPermissionId.HasValue && userPermissionIds.Contains(item.UserPermissionId.Value),
                item => item.ConditionGroup,
                cancellationToken);
        }

        return [];
    }

    private async Task<List<PermissionDelegationListItemDto>> GetDelegationsAsync(
        SysPermission permission,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var delegations = await _permissionDelegationRepository.GetListAsync(
            item => item.PermissionId == permission.BasicId,
            item => item.CreatedTime,
            cancellationToken);

        if (delegations.Count == 0)
        {
            return [];
        }

        var tenantMemberMap = await BuildTenantMemberMapAsync(
            delegations.SelectMany(item => new[] { item.DelegatorUserId, item.DelegateeUserId }),
            cancellationToken);
        var roleMap = await BuildRoleMapAsync(
            delegations
                .Where(item => item.RoleId.HasValue)
                .Select(item => item.RoleId!.Value),
            cancellationToken);

        return [.. delegations
            .Select(item => PermissionDelegationApplicationMapper.ToListItemDto(
                item,
                tenantMemberMap.GetValueOrDefault(item.DelegatorUserId),
                tenantMemberMap.GetValueOrDefault(item.DelegateeUserId),
                permission,
                item.RoleId.HasValue ? roleMap.GetValueOrDefault(item.RoleId.Value) : null,
                now))
            .OrderByDescending(item => item.CreatedTime)
            .ThenBy(item => item.BasicId)
            .Take(MaxDelegationCount)];
    }

    private async Task<List<PermissionRequestListItemDto>> GetRequestsAsync(
        SysPermission permission,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var requests = await _permissionRequestRepository.GetListAsync(
            item => item.PermissionId == permission.BasicId,
            item => item.CreatedTime,
            cancellationToken);

        if (requests.Count == 0)
        {
            return [];
        }

        var tenantMemberMap = await BuildTenantMemberMapAsync(requests.Select(item => item.RequestUserId), cancellationToken);
        var roleMap = await BuildRoleMapAsync(
            requests
                .Where(item => item.RoleId.HasValue)
                .Select(item => item.RoleId!.Value),
            cancellationToken);
        var reviewMap = await BuildReviewMapAsync(
            requests
                .Where(item => item.ReviewId.HasValue)
                .Select(item => item.ReviewId!.Value),
            cancellationToken);

        return [.. requests
            .Select(item => PermissionRequestApplicationMapper.ToListItemDto(
                item,
                tenantMemberMap.GetValueOrDefault(item.RequestUserId),
                permission,
                item.RoleId.HasValue ? roleMap.GetValueOrDefault(item.RoleId.Value) : null,
                item.ReviewId.HasValue ? reviewMap.GetValueOrDefault(item.ReviewId.Value) : null,
                now))
            .OrderByDescending(item => item.CreatedTime)
            .ThenBy(item => item.BasicId)
            .Take(MaxRequestCount)];
    }

    private async Task<List<FieldLevelSecurityListItemDto>> GetFieldSecuritiesAsync(SysPermission permission, CancellationToken cancellationToken)
    {
        IReadOnlyList<SysFieldLevelSecurity> policies;
        if (permission.ResourceId.HasValue)
        {
            var resourceId = permission.ResourceId.Value;
            policies = await _fieldLevelSecurityRepository.GetListAsync(
                item => (item.TargetType == FieldSecurityTargetType.Permission && item.TargetId == permission.BasicId)
                    || item.ResourceId == resourceId,
                item => item.CreatedTime,
                cancellationToken);
        }
        else
        {
            policies = await _fieldLevelSecurityRepository.GetListAsync(
                item => item.TargetType == FieldSecurityTargetType.Permission && item.TargetId == permission.BasicId,
                item => item.CreatedTime,
                cancellationToken);
        }

        if (policies.Count == 0)
        {
            return [];
        }

        var resourceMap = await BuildResourceMapAsync(policies.Select(item => item.ResourceId), cancellationToken);
        var roleMap = await BuildRoleMapAsync(
            policies
                .Where(item => item.TargetType == FieldSecurityTargetType.Role)
                .Select(item => item.TargetId),
            cancellationToken);
        var permissionMap = await BuildPermissionMapAsync(
            policies
                .Where(item => item.TargetType == FieldSecurityTargetType.Permission)
                .Select(item => item.TargetId),
            cancellationToken);
        var departmentMap = await BuildDepartmentMapAsync(
            policies
                .Where(item => item.TargetType == FieldSecurityTargetType.Department)
                .Select(item => item.TargetId),
            cancellationToken);
        var tenantMemberMap = await BuildTenantMemberMapAsync(
            policies
                .Where(item => item.TargetType == FieldSecurityTargetType.User)
                .Select(item => item.TargetId),
            cancellationToken);

        return [.. policies
            .Select(item =>
            {
                var (targetCode, targetName) = ResolveFieldSecurityTarget(item, roleMap, permissionMap, departmentMap, tenantMemberMap);
                return FieldLevelSecurityApplicationMapper.ToListItemDto(
                    item,
                    resourceMap.GetValueOrDefault(item.ResourceId),
                    targetCode,
                    targetName);
            })
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.ResourceCode)
            .ThenBy(item => item.FieldName)
            .ThenBy(item => item.BasicId)
            .Take(MaxFieldSecurityCount)];
    }

    private async Task<List<PermissionChangeLogListItemDto>> GetChangeLogsAsync(long permissionId, CancellationToken cancellationToken)
    {
        var logs = await DbClient.Queryable<SysPermissionChangeLog>()
            .Where(item => item.PermissionId == permissionId)
            .SplitTable()
            .OrderByDescending(item => item.ChangeTime)
            .Take(MaxChangeLogCount)
            .ToListAsync(cancellationToken);

        return [.. logs.Select(PermissionChangeLogApplicationMapper.ToListItemDto)];
    }

    private static (string? Code, string? Name) ResolveFieldSecurityTarget(
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

    private async Task<IReadOnlyDictionary<long, SysPermission>> BuildPermissionMapAsync(IEnumerable<long> permissionIds, CancellationToken cancellationToken)
    {
        var ids = permissionIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysPermission>();
        }

        var permissions = await _permissionRepository.GetByIdsAsync(ids, cancellationToken);
        return permissions.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysResource>> BuildResourceMapAsync(IEnumerable<long> resourceIds, CancellationToken cancellationToken)
    {
        var ids = resourceIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysResource>();
        }

        var resources = await _resourceRepository.GetByIdsAsync(ids, cancellationToken);
        return resources.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysRole>> BuildRoleMapAsync(IEnumerable<long> roleIds, CancellationToken cancellationToken)
    {
        var ids = roleIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysRole>();
        }

        var roles = await _roleRepository.GetByIdsAsync(ids, cancellationToken);
        return roles.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysDepartment>> BuildDepartmentMapAsync(IEnumerable<long> departmentIds, CancellationToken cancellationToken)
    {
        var ids = departmentIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysDepartment>();
        }

        var departments = await _departmentRepository.GetByIdsAsync(ids, cancellationToken);
        return departments.ToDictionary(item => item.BasicId);
    }

    private async Task<IReadOnlyDictionary<long, SysTenantUser>> BuildTenantMemberMapAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysTenantUser>();
        }

        var tenantMembers = await _tenantUserRepository.GetListAsync(
            item => ids.Contains(item.UserId),
            item => item.CreatedTime,
            cancellationToken);
        return tenantMembers.ToDictionary(item => item.UserId);
    }

    private async Task<IReadOnlyDictionary<long, SysReview>> BuildReviewMapAsync(IEnumerable<long> reviewIds, CancellationToken cancellationToken)
    {
        var ids = reviewIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysReview>();
        }

        var reviews = await _reviewRepository.GetByIdsAsync(ids, cancellationToken);
        return reviews.ToDictionary(item => item.BasicId);
    }
}
