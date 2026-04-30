#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestAppService
// Guid:49fd9d83-dfa1-4b4b-898c-3f59cdf9b930
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
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限申请命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限申请")]
public sealed class PermissionRequestAppService(
    IPermissionRequestRepository permissionRequestRepository,
    ITenantUserRepository tenantUserRepository,
    IPermissionRepository permissionRepository,
    IRoleRepository roleRepository,
    IReviewRepository reviewRepository,
    ICurrentUser currentUser)
    : SaasApplicationService, IPermissionRequestAppService
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
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser = currentUser;

    /// <summary>
    /// 创建权限申请
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Create)]
    public async Task<PermissionRequestDetailDto> CreatePermissionRequestAsync(PermissionRequestCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateCreateInput(input, now);

        var requestUserId = GetCurrentUserIdOrThrow();
        var requestUser = await GetAvailableTenantMemberOrThrowAsync(requestUserId, now, cancellationToken);
        var permission = await GetRequestablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetRequestableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsurePendingRequestNotExistsAsync(requestUserId, input.PermissionId, input.RoleId, null, cancellationToken);

        var permissionRequest = new SysPermissionRequest
        {
            RequestUserId = requestUserId,
            PermissionId = input.PermissionId,
            RoleId = input.RoleId,
            RequestReason = input.RequestReason.Trim(),
            ExpectedEffectiveTime = input.ExpectedEffectiveTime,
            ExpectedExpirationTime = input.ExpectedExpirationTime,
            RequestStatus = PermissionRequestStatus.Pending,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedRequest = await _permissionRequestRepository.AddAsync(permissionRequest, cancellationToken);
        return PermissionRequestApplicationMapper.ToDetailDto(savedRequest, requestUser, permission, role, null, now);
    }

    /// <summary>
    /// 更新权限申请
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Update)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestAsync(PermissionRequestUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateUpdateInput(input, now);

        var requestUserId = GetCurrentUserIdOrThrow();
        var permissionRequest = await GetPermissionRequestOrThrowAsync(input.BasicId, cancellationToken);
        EnsurePendingOwnerRequest(permissionRequest, requestUserId);

        var requestUser = await GetAvailableTenantMemberOrThrowAsync(requestUserId, now, cancellationToken);
        var permission = await GetRequestablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetRequestableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsurePendingRequestNotExistsAsync(requestUserId, input.PermissionId, input.RoleId, permissionRequest.BasicId, cancellationToken);

        permissionRequest.PermissionId = input.PermissionId;
        permissionRequest.RoleId = input.RoleId;
        permissionRequest.RequestReason = input.RequestReason.Trim();
        permissionRequest.ExpectedEffectiveTime = input.ExpectedEffectiveTime;
        permissionRequest.ExpectedExpirationTime = input.ExpectedExpirationTime;
        permissionRequest.Remark = NormalizeNullable(input.Remark);

        var savedRequest = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
        return PermissionRequestApplicationMapper.ToDetailDto(savedRequest, requestUser, permission, role, null, now);
    }

    /// <summary>
    /// 更新权限申请状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限申请详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Status)]
    public async Task<PermissionRequestDetailDto> UpdatePermissionRequestStatusAsync(PermissionRequestStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限申请主键必须大于 0。");
        }

        ValidateEnum(input.RequestStatus, nameof(input.RequestStatus));
        ValidateOptionalId(input.ReviewId, nameof(input.ReviewId), "审批单主键必须大于 0。");

        _ = await GetAvailableTenantMemberOrThrowAsync(GetCurrentUserIdOrThrow(), DateTimeOffset.UtcNow, cancellationToken);
        var permissionRequest = await GetPermissionRequestOrThrowAsync(input.BasicId, cancellationToken);
        EnsureStatusCanBeChanged(permissionRequest, input.RequestStatus);

        var review = input.ReviewId.HasValue
            ? await GetEnabledReviewOrThrowAsync(input.ReviewId.Value, cancellationToken)
            : await GetReviewOrDefaultAsync(permissionRequest.ReviewId, cancellationToken);

        permissionRequest.RequestStatus = input.RequestStatus;
        permissionRequest.ReviewId = input.ReviewId ?? permissionRequest.ReviewId;
        permissionRequest.Remark = NormalizeNullable(input.Remark);

        var requestUser = await _tenantUserRepository.GetMembershipAsync(permissionRequest.RequestUserId, cancellationToken);
        var permission = await GetPermissionOrDefaultAsync(permissionRequest.PermissionId, cancellationToken);
        var role = await GetRoleOrDefaultAsync(permissionRequest.RoleId, cancellationToken);
        var savedRequest = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
        return PermissionRequestApplicationMapper.ToDetailDto(savedRequest, requestUser, permission, role, review, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 撤回权限申请
    /// </summary>
    /// <param name="id">权限申请主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionRequest.Withdraw)]
    public async Task DeletePermissionRequestAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestUserId = GetCurrentUserIdOrThrow();
        var permissionRequest = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        EnsurePendingOwnerRequest(permissionRequest, requestUserId);

        permissionRequest.RequestStatus = PermissionRequestStatus.Withdrawn;
        _ = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
    }

    /// <summary>
    /// 获取权限申请，不存在时抛出异常
    /// </summary>
    private async Task<SysPermissionRequest> GetPermissionRequestOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限申请主键必须大于 0。");
        }

        return await _permissionRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

    /// <summary>
    /// 获取当前用户主键，不存在时抛出异常
    /// </summary>
    private long GetCurrentUserIdOrThrow()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
        {
            throw new InvalidOperationException("当前用户未登录。");
        }

        return _currentUser.UserId.Value;
    }

    /// <summary>
    /// 获取可提交申请的当前租户成员
    /// </summary>
    private async Task<SysTenantUser> GetAvailableTenantMemberOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能提交权限申请。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能提交权限申请。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员权限申请必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能提交权限申请。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能提交权限申请。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 获取可申请权限
    /// </summary>
    private async Task<SysPermission?> GetRequestablePermissionOrDefaultAsync(long? permissionId, CancellationToken cancellationToken)
    {
        if (!permissionId.HasValue)
        {
            return null;
        }

        var permission = await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能申请。");
        }

        return permission;
    }

    /// <summary>
    /// 获取可申请角色
    /// </summary>
    private async Task<SysRole?> GetRequestableRoleOrDefaultAsync(long? roleId, CancellationToken cancellationToken)
    {
        if (!roleId.HasValue)
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(roleId.Value, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能申请。");
        }

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色权限申请必须通过平台运维流程维护。");
        }

        return role;
    }

    /// <summary>
    /// 获取启用审批单
    /// </summary>
    private async Task<SysReview> GetEnabledReviewOrThrowAsync(long reviewId, CancellationToken cancellationToken)
    {
        var review = await _reviewRepository.GetByIdAsync(reviewId, cancellationToken)
            ?? throw new InvalidOperationException("审批单不存在。");

        if (review.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用审批单不能关联权限申请。");
        }

        return review;
    }

    /// <summary>
    /// 按需获取权限
    /// </summary>
    private async Task<SysPermission?> GetPermissionOrDefaultAsync(long? permissionId, CancellationToken cancellationToken)
    {
        return permissionId.HasValue
            ? await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken)
            : null;
    }

    /// <summary>
    /// 按需获取角色
    /// </summary>
    private async Task<SysRole?> GetRoleOrDefaultAsync(long? roleId, CancellationToken cancellationToken)
    {
        return roleId.HasValue
            ? await _roleRepository.GetByIdAsync(roleId.Value, cancellationToken)
            : null;
    }

    /// <summary>
    /// 按需获取审批单
    /// </summary>
    private async Task<SysReview?> GetReviewOrDefaultAsync(long? reviewId, CancellationToken cancellationToken)
    {
        return reviewId.HasValue
            ? await _reviewRepository.GetByIdAsync(reviewId.Value, cancellationToken)
            : null;
    }

    /// <summary>
    /// 校验待审批申请不存在
    /// </summary>
    private async Task EnsurePendingRequestNotExistsAsync(
        long requestUserId,
        long? permissionId,
        long? roleId,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _permissionRequestRepository.AnyAsync(
                request => request.RequestUserId == requestUserId
                    && request.PermissionId == permissionId
                    && request.RoleId == roleId
                    && request.RequestStatus == PermissionRequestStatus.Pending
                    && request.BasicId != excludeId.Value,
                cancellationToken)
            : await _permissionRequestRepository.AnyAsync(
                request => request.RequestUserId == requestUserId
                    && request.PermissionId == permissionId
                    && request.RoleId == roleId
                    && request.RequestStatus == PermissionRequestStatus.Pending,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("相同权限或角色的待审批申请已存在。");
        }
    }

    /// <summary>
    /// 校验待审批本人申请
    /// </summary>
    private static void EnsurePendingOwnerRequest(SysPermissionRequest permissionRequest, long currentUserId)
    {
        if (permissionRequest.RequestUserId != currentUserId)
        {
            throw new InvalidOperationException("只能维护自己的权限申请。");
        }

        if (permissionRequest.RequestStatus != PermissionRequestStatus.Pending)
        {
            throw new InvalidOperationException("只有待审批权限申请可以维护。");
        }
    }

    /// <summary>
    /// 校验状态变更
    /// </summary>
    private static void EnsureStatusCanBeChanged(SysPermissionRequest permissionRequest, PermissionRequestStatus nextStatus)
    {
        if (nextStatus == PermissionRequestStatus.Approved)
        {
            throw new InvalidOperationException("权限申请审批通过必须走审批流并自动授权，不能直接更新为已批准。");
        }

        if (permissionRequest.RequestStatus != PermissionRequestStatus.Pending && permissionRequest.RequestStatus != nextStatus)
        {
            throw new InvalidOperationException("已完结权限申请不能变更状态。");
        }
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(PermissionRequestCreateDto input, DateTimeOffset now)
    {
        ValidateCommonInput(input.PermissionId, input.RoleId, input.RequestReason, input.ExpectedEffectiveTime, input.ExpectedExpirationTime, now);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(PermissionRequestUpdateDto input, DateTimeOffset now)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限申请主键必须大于 0。");
        }

        ValidateCommonInput(input.PermissionId, input.RoleId, input.RequestReason, input.ExpectedEffectiveTime, input.ExpectedExpirationTime, now);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
        long? permissionId,
        long? roleId,
        string requestReason,
        DateTimeOffset? expectedEffectiveTime,
        DateTimeOffset? expectedExpirationTime,
        DateTimeOffset now)
    {
        if (!permissionId.HasValue && !roleId.HasValue)
        {
            throw new InvalidOperationException("权限申请必须指定权限或角色。");
        }

        ValidateOptionalId(permissionId, nameof(permissionId), "权限主键必须大于 0。");
        ValidateOptionalId(roleId, nameof(roleId), "角色主键必须大于 0。");
        ArgumentException.ThrowIfNullOrWhiteSpace(requestReason);

        if (requestReason.Trim().Length > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(requestReason), "申请原因不能超过 1000 个字符。");
        }

        if (expectedExpirationTime.HasValue && expectedExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("期望失效时间必须晚于当前时间。");
        }

        if (expectedEffectiveTime.HasValue && expectedExpirationTime.HasValue && expectedExpirationTime.Value <= expectedEffectiveTime.Value)
        {
            throw new InvalidOperationException("期望失效时间必须晚于期望生效时间。");
        }
    }

    /// <summary>
    /// 校验可选主键
    /// </summary>
    private static void ValidateOptionalId(long? id, string paramName, string message)
    {
        if (id.HasValue && id.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    /// <param name="value">枚举值</param>
    /// <param name="paramName">参数名</param>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <returns>规范化后的字符串</returns>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
