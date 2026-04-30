#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestQueryService
// Guid:030e91ae-f3c7-499c-ab8c-7241bb0a9857
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
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 权限申请查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限申请")]
public sealed class PermissionRequestQueryService(
    IPermissionRequestRepository permissionRequestRepository,
    ITenantUserRepository tenantUserRepository,
    IPermissionRepository permissionRepository,
    IRoleRepository roleRepository,
    IReviewRepository reviewRepository)
    : SaasApplicationService, IPermissionRequestQueryService
{
    /// <summary>
    /// 权限申请仓储
    /// </summary>
    private readonly IPermissionRequestRepository _permissionRequestRepository = permissionRequestRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 审批仓储
    /// </summary>
    private readonly IReviewRepository _reviewRepository = reviewRepository;

    /// <summary>
    /// 获取权限申请分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Read)]
    public async Task<PageResultDtoBase<PermissionRequestListItemDto>> GetPermissionRequestPageAsync(PermissionRequestPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildPermissionRequestPageRequest(input);
        var permissionRequests = await _permissionRequestRepository.GetPagedAsync(request, cancellationToken);
        if (permissionRequests.Items.Count == 0)
        {
            return new PageResultDtoBase<PermissionRequestListItemDto>([], permissionRequests.Page);
        }

        var now = DateTimeOffset.UtcNow;
        var tenantMemberMap = await BuildTenantMemberMapAsync(permissionRequests.Items.Select(item => item.RequestUserId), cancellationToken);
        var permissionMap = await BuildPermissionMapAsync(
            permissionRequests.Items
                .Where(item => item.PermissionId.HasValue)
                .Select(item => item.PermissionId!.Value),
            cancellationToken);
        var roleMap = await BuildRoleMapAsync(
            permissionRequests.Items
                .Where(item => item.RoleId.HasValue)
                .Select(item => item.RoleId!.Value),
            cancellationToken);
        var reviewMap = await BuildReviewMapAsync(
            permissionRequests.Items
                .Where(item => item.ReviewId.HasValue)
                .Select(item => item.ReviewId!.Value),
            cancellationToken);

        var items = permissionRequests.Items
            .Select(item => PermissionRequestApplicationMapper.ToListItemDto(
                item,
                tenantMemberMap.GetValueOrDefault(item.RequestUserId),
                item.PermissionId.HasValue ? permissionMap.GetValueOrDefault(item.PermissionId.Value) : null,
                item.RoleId.HasValue ? roleMap.GetValueOrDefault(item.RoleId.Value) : null,
                item.ReviewId.HasValue ? reviewMap.GetValueOrDefault(item.ReviewId.Value) : null,
                now))
            .ToList();

        return new PageResultDtoBase<PermissionRequestListItemDto>(items, permissionRequests.Page);
    }

    /// <summary>
    /// 获取权限申请详情
    /// </summary>
    /// <param name="id">权限申请主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Read)]
    public async Task<PermissionRequestDetailDto?> GetPermissionRequestDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限申请主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var permissionRequest = await _permissionRequestRepository.GetByIdAsync(id, cancellationToken);
        if (permissionRequest is null)
        {
            return null;
        }

        var requestUser = await _tenantUserRepository.GetMembershipAsync(permissionRequest.RequestUserId, cancellationToken);
        var permission = permissionRequest.PermissionId.HasValue
            ? await _permissionRepository.GetByIdAsync(permissionRequest.PermissionId.Value, cancellationToken)
            : null;
        var role = permissionRequest.RoleId.HasValue
            ? await _roleRepository.GetByIdAsync(permissionRequest.RoleId.Value, cancellationToken)
            : null;
        var review = permissionRequest.ReviewId.HasValue
            ? await _reviewRepository.GetByIdAsync(permissionRequest.ReviewId.Value, cancellationToken)
            : null;

        return PermissionRequestApplicationMapper.ToDetailDto(permissionRequest, requestUser, permission, role, review, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建权限申请分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>权限申请分页请求</returns>
    private static BasicAppPRDto BuildPermissionRequestPageRequest(PermissionRequestPageQueryDto input)
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
                nameof(SysPermissionRequest.RequestReason),
                nameof(SysPermissionRequest.Remark));
        }

        if (input.RequestUserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionRequest.RequestUserId), input.RequestUserId.Value);
        }

        if (input.PermissionId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionRequest.PermissionId), input.PermissionId.Value);
        }

        if (input.RoleId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionRequest.RoleId), input.RoleId.Value);
        }

        if (input.ReviewId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionRequest.ReviewId), input.ReviewId.Value);
        }

        if (input.RequestStatus.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysPermissionRequest.RequestStatus), input.RequestStatus.Value);
        }

        request.Conditions.AddSort(nameof(SysPermissionRequest.CreatedTime), SortDirection.Descending, 0);
        return request;
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
    /// 构建审批单映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysReview>> BuildReviewMapAsync(IEnumerable<long> reviewIds, CancellationToken cancellationToken)
    {
        var ids = reviewIds
            .Where(reviewId => reviewId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysReview>();
        }

        var reviews = await _reviewRepository.GetByIdsAsync(ids, cancellationToken);
        return reviews.ToDictionary(review => review.BasicId);
    }
}
