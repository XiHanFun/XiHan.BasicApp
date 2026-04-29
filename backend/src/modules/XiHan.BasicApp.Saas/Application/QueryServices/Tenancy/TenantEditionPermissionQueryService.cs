#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEditionPermissionQueryService
// Guid:d17bdc0b-4111-49b8-b5a9-2848dc9d1e80
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
/// 租户版本权限查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "租户版本权限")]
public sealed class TenantEditionPermissionQueryService(
    ITenantEditionRepository tenantEditionRepository,
    ITenantEditionPermissionRepository tenantEditionPermissionRepository,
    IPermissionRepository permissionRepository)
    : SaasApplicationService, ITenantEditionPermissionQueryService
{
    /// <summary>
    /// 租户版本仓储
    /// </summary>
    private readonly ITenantEditionRepository _tenantEditionRepository = tenantEditionRepository;

    /// <summary>
    /// 租户版本权限仓储
    /// </summary>
    private readonly ITenantEditionPermissionRepository _tenantEditionPermissionRepository = tenantEditionPermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 获取租户版本权限列表
    /// </summary>
    /// <param name="editionId">租户版本主键</param>
    /// <param name="onlyValid">是否仅返回有效绑定</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Read)]
    public async Task<IReadOnlyList<TenantEditionPermissionListItemDto>> GetTenantEditionPermissionsAsync(long editionId, bool onlyValid = false, CancellationToken cancellationToken = default)
    {
        if (editionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(editionId), "租户版本主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        _ = await _tenantEditionRepository.GetByIdAsync(editionId, cancellationToken)
            ?? throw new InvalidOperationException("租户版本不存在。");

        var editionPermissions = onlyValid
            ? await _tenantEditionPermissionRepository.GetListAsync(
                editionPermission => editionPermission.EditionId == editionId && editionPermission.Status == ValidityStatus.Valid,
                editionPermission => editionPermission.CreatedTime,
                cancellationToken)
            : await _tenantEditionPermissionRepository.GetListAsync(
                editionPermission => editionPermission.EditionId == editionId,
                editionPermission => editionPermission.CreatedTime,
                cancellationToken);

        if (editionPermissions.Count == 0)
        {
            return [];
        }

        var permissionMap = await BuildPermissionMapAsync(editionPermissions.Select(editionPermission => editionPermission.PermissionId), cancellationToken);

        return [.. editionPermissions
            .Select(editionPermission => TenantEditionPermissionApplicationMapper.ToListItemDto(
                editionPermission,
                permissionMap.GetValueOrDefault(editionPermission.PermissionId)))
            .OrderBy(item => item.PermissionCode)
            .ThenBy(item => item.PermissionId)];
    }

    /// <summary>
    /// 获取租户版本权限详情
    /// </summary>
    /// <param name="id">租户版本权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户版本权限详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.TenantEditionPermission.Read)]
    public async Task<TenantEditionPermissionDetailDto?> GetTenantEditionPermissionDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "租户版本权限绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var editionPermission = await _tenantEditionPermissionRepository.GetByIdAsync(id, cancellationToken);
        if (editionPermission is null)
        {
            return null;
        }

        var permission = await _permissionRepository.GetByIdAsync(editionPermission.PermissionId, cancellationToken);
        return TenantEditionPermissionApplicationMapper.ToDetailDto(editionPermission, permission);
    }

    /// <summary>
    /// 构建权限定义映射
    /// </summary>
    /// <param name="permissionIds">权限主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限定义映射</returns>
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
}
