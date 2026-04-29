#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionQueryService
// Guid:1e7f9ca4-a7bc-4ae2-bb8c-b35aaef74c47
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
/// 权限查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限")]
public sealed class PermissionQueryService(
    IPermissionRepository permissionRepository,
    IResourceRepository resourceRepository,
    IOperationRepository operationRepository)
    : SaasApplicationService, IPermissionQueryService
{
    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 资源仓储
    /// </summary>
    private readonly IResourceRepository _resourceRepository = resourceRepository;

    /// <summary>
    /// 操作仓储
    /// </summary>
    private readonly IOperationRepository _operationRepository = operationRepository;

    /// <summary>
    /// 获取权限分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Permission.Read)]
    public async Task<PageResultDtoBase<PermissionListItemDto>> GetPermissionPageAsync(PermissionPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPermissionPageRequest(input);
        var permissions = await _permissionRepository.GetPagedAsync(request, cancellationToken);
        if (permissions.Items.Count == 0)
        {
            return new PageResultDtoBase<PermissionListItemDto>([], permissions.Page)
            {
                ExtendDatas = permissions.ExtendDatas
            };
        }

        var resourceMap = await BuildResourceMapAsync(permissions.Items.Select(permission => permission.ResourceId), cancellationToken);
        var operationMap = await BuildOperationMapAsync(permissions.Items.Select(permission => permission.OperationId), cancellationToken);
        var items = permissions.Items
            .Select(permission => PermissionApplicationMapper.ToListItemDto(
                permission,
                TryGetMapValue(resourceMap, permission.ResourceId),
                TryGetMapValue(operationMap, permission.OperationId)))
            .ToList();

        return new PageResultDtoBase<PermissionListItemDto>(items, permissions.Page)
        {
            ExtendDatas = permissions.ExtendDatas
        };
    }

    /// <summary>
    /// 获取权限详情
    /// </summary>
    /// <param name="id">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Permission.Read)]
    public async Task<PermissionDetailDto?> GetPermissionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var permission = await _permissionRepository.GetByIdAsync(id, cancellationToken);
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

        return PermissionApplicationMapper.ToDetailDto(permission, resource, operation);
    }

    /// <summary>
    /// 获取可选全局权限列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可选全局权限列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Permission.Read)]
    public async Task<IReadOnlyList<PermissionSelectItemDto>> GetAvailableGlobalPermissionsAsync(PermissionSelectQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPermissionSelectRequest(input);
        var permissions = await _permissionRepository.GetPagedAsync(request, cancellationToken);

        return [.. permissions.Items.Select(PermissionApplicationMapper.ToSelectItemDto)];
    }

    /// <summary>
    /// 构建权限分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>权限分页请求</returns>
    private static BasicAppPRDto BuildPermissionPageRequest(PermissionPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        ApplyCommonPermissionFilters(
            request,
            input.Keyword,
            input.ModuleCode,
            input.PermissionType,
            input.ResourceId,
            input.OperationId,
            input.IsGlobal,
            input.IsRequireAudit,
            input.Status);

        request.Conditions.AddSort(nameof(SysPermission.ModuleCode), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysPermission.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysPermission.PermissionCode), SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 构建权限选择请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>权限选择请求</returns>
    private static BasicAppPRDto BuildPermissionSelectRequest(PermissionSelectQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Conditions = new QueryConditions()
        };

        request.Page.PageSize = Math.Clamp(input.Limit, 1, 500);

        ApplyCommonPermissionFilters(
            request,
            input.Keyword,
            input.ModuleCode,
            input.PermissionType,
            resourceId: null,
            operationId: null,
            isGlobal: true,
            isRequireAudit: null,
            status: EnableStatus.Enabled);

        request.Conditions.AddSort(nameof(SysPermission.ModuleCode), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysPermission.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysPermission.PermissionCode), SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 应用权限通用筛选条件
    /// </summary>
    private static void ApplyCommonPermissionFilters(
        BasicAppPRDto request,
        string? keyword,
        string? moduleCode,
        PermissionType? permissionType,
        long? resourceId,
        long? operationId,
        bool? isGlobal,
        bool? isRequireAudit,
        EnableStatus? status)
    {
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            request.Conditions.SetKeyword(
                keyword.Trim(),
                nameof(SysPermission.PermissionCode),
                nameof(SysPermission.PermissionName),
                nameof(SysPermission.PermissionDescription),
                nameof(SysPermission.Tags));
        }

        if (!string.IsNullOrWhiteSpace(moduleCode))
        {
            request.Conditions.AddFilter(nameof(SysPermission.ModuleCode), moduleCode.Trim());
        }

        if (permissionType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermission.PermissionType), permissionType.Value);
        }

        if (resourceId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermission.ResourceId), resourceId.Value);
        }

        if (operationId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermission.OperationId), operationId.Value);
        }

        if (isGlobal.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermission.IsGlobal), isGlobal.Value);
        }

        if (isRequireAudit.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermission.IsRequireAudit), isRequireAudit.Value);
        }

        if (status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermission.Status), status.Value);
        }
    }

    /// <summary>
    /// 构建资源定义映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysResource>> BuildResourceMapAsync(IEnumerable<long?> resourceIds, CancellationToken cancellationToken)
    {
        var ids = resourceIds
            .Where(resourceId => resourceId.HasValue && resourceId.Value > 0)
            .Select(resourceId => resourceId!.Value)
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
    /// 构建操作定义映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysOperation>> BuildOperationMapAsync(IEnumerable<long?> operationIds, CancellationToken cancellationToken)
    {
        var ids = operationIds
            .Where(operationId => operationId.HasValue && operationId.Value > 0)
            .Select(operationId => operationId!.Value)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysOperation>();
        }

        var operations = await _operationRepository.GetByIdsAsync(ids, cancellationToken);
        return operations.ToDictionary(operation => operation.BasicId);
    }

    /// <summary>
    /// 从可空主键映射中读取实体
    /// </summary>
    private static TValue? TryGetMapValue<TValue>(IReadOnlyDictionary<long, TValue> map, long? id)
        where TValue : class
    {
        return id.HasValue && map.TryGetValue(id.Value, out var value) ? value : null;
    }
}
