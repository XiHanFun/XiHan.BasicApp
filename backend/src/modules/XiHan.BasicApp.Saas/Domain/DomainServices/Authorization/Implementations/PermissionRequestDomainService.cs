#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionRequestDomainService
// Guid:cae77906-e146-4fae-a90d-e03499cc8081
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限申请领域服务实现
/// </summary>
public sealed class PermissionRequestDomainService
    : IPermissionRequestDomainService
{
    private const string PermissionRequestReviewType = "PermissionRequest";

    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionRequestRepository _permissionRequestRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IReviewDomainService _reviewDomainService;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionRequestDomainService(
        IPermissionRequestRepository permissionRequestRepository,
        ITenantUserRepository tenantUserRepository,
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IReviewRepository reviewRepository,
        IReviewDomainService reviewDomainService,
        IUserRoleRepository userRoleRepository,
        IUserPermissionRepository userPermissionRepository,
        ICurrentTenant currentTenant)
    {
        _permissionRequestRepository = permissionRequestRepository;
        _tenantUserRepository = tenantUserRepository;
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _reviewRepository = reviewRepository;
        _reviewDomainService = reviewDomainService;
        _userRoleRepository = userRoleRepository;
        _userPermissionRepository = userPermissionRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<PermissionRequestCommandResult> CreatePermissionRequestAsync(PermissionRequestCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateRequestCommonInput(
            command.PermissionId,
            command.RoleId,
            command.RequestReason,
            command.ExpectedEffectiveTime,
            command.ExpectedExpirationTime,
            now);

        _ = await GetAvailableTenantMemberForRequestOrThrowAsync(command.RequestUserId, now, cancellationToken);
        _ = await GetRequestablePermissionOrDefaultAsync(command.PermissionId, cancellationToken);
        _ = await GetRequestableRoleOrDefaultAsync(command.RoleId, cancellationToken);
        await EnsurePendingRequestNotExistsAsync(command.RequestUserId, command.PermissionId, command.RoleId, null, cancellationToken);

        var permissionRequest = new SysPermissionRequest
        {
            RequestUserId = command.RequestUserId,
            PermissionId = command.PermissionId,
            RoleId = command.RoleId,
            RequestReason = command.RequestReason.Trim(),
            ExpectedEffectiveTime = command.ExpectedEffectiveTime,
            ExpectedExpirationTime = command.ExpectedExpirationTime,
            RequestStatus = PermissionRequestStatus.Pending,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedRequest = await _permissionRequestRepository.AddAsync(permissionRequest, cancellationToken);

        // 同步创建审批单（审批流入口），并回填 ReviewId：用户申请 → 审批 → 自动授权
        var review = new SysReview
        {
            ReviewCode = $"PR-{savedRequest.BasicId}",
            ReviewTitle = BuildReviewTitle(command.PermissionId, command.RoleId),
            ReviewType = PermissionRequestReviewType,
            EntityType = nameof(SysPermissionRequest),
            EntityId = savedRequest.BasicId.ToString(),
            ReviewStatus = AuditStatus.Pending,
            Priority = 0,
            SubmitUserId = command.RequestUserId,
            SubmitTime = now,
            ReviewLevel = 1,
            CurrentLevel = 1,
            Status = EnableStatus.Enabled
        };
        var savedReview = await _reviewRepository.AddAsync(review, cancellationToken);

        savedRequest.ReviewId = savedReview.BasicId;
        savedRequest = await _permissionRequestRepository.UpdateAsync(savedRequest, cancellationToken);

        return new PermissionRequestCommandResult(savedRequest.BasicId);
    }

    /// <inheritdoc />
    public async Task<PermissionRequestCommandResult> ApprovePermissionRequestAsync(PermissionRequestApprovalCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限申请主键必须大于 0。");
        }

        var now = DateTimeOffset.UtcNow;
        _ = await GetAvailableTenantMemberForRequestOrThrowAsync(command.OperatorUserId, now, cancellationToken);
        var request = await GetPermissionRequestOrThrowAsync(command.BasicId, cancellationToken);
        if (request.RequestStatus != PermissionRequestStatus.Pending)
        {
            throw new InvalidOperationException("只有待审批权限申请可以审批。");
        }

        // 重新校验申请对象仍可用（防止审批时权限/角色已被停用）
        _ = await GetRequestablePermissionOrDefaultAsync(request.PermissionId, cancellationToken);
        _ = await GetRequestableRoleOrDefaultAsync(request.RoleId, cancellationToken);

        // 自动授权：为申请人创建角色 / 权限直授（沿用申请的期望有效期）
        var grantReason = $"权限申请[{request.BasicId}]审批通过自动授权";
        if (request.RoleId is > 0)
        {
            await _userRoleRepository.AddAsync(new SysUserRole
            {
                UserId = request.RequestUserId,
                RoleId = request.RoleId.Value,
                EffectiveTime = request.ExpectedEffectiveTime,
                ExpirationTime = request.ExpectedExpirationTime,
                Status = ValidityStatus.Valid,
                GrantReason = grantReason
            }, cancellationToken);
        }

        if (request.PermissionId is > 0)
        {
            await _userPermissionRepository.AddAsync(new SysUserPermission
            {
                UserId = request.RequestUserId,
                PermissionId = request.PermissionId.Value,
                PermissionAction = PermissionAction.Grant,
                EffectiveTime = request.ExpectedEffectiveTime,
                ExpirationTime = request.ExpectedExpirationTime,
                Status = ValidityStatus.Valid,
                GrantReason = grantReason
            }, cancellationToken);
        }

        // 审批单留痕（通过）；单级审批 → Approved
        if (request.ReviewId is > 0)
        {
            await _reviewDomainService.AuditReviewAsync(
                new ReviewAuditCommand(
                    request.ReviewId.Value,
                    AuditResult.Pass,
                    command.OperatorUserId,
                    null,
                    command.Remark,
                    "Approve",
                    now,
                    null,
                    null,
                    command.Remark,
                    command.OperatorUserId),
                cancellationToken);
        }

        request.RequestStatus = PermissionRequestStatus.Approved;
        request.Remark = NormalizeNullable(command.Remark) ?? request.Remark;
        var savedRequest = await _permissionRequestRepository.UpdateAsync(request, cancellationToken);
        return new PermissionRequestCommandResult(savedRequest.BasicId);
    }

    /// <inheritdoc />
    public async Task<PermissionRequestCommandResult> RejectPermissionRequestAsync(PermissionRequestApprovalCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限申请主键必须大于 0。");
        }

        var now = DateTimeOffset.UtcNow;
        _ = await GetAvailableTenantMemberForRequestOrThrowAsync(command.OperatorUserId, now, cancellationToken);
        var request = await GetPermissionRequestOrThrowAsync(command.BasicId, cancellationToken);
        if (request.RequestStatus != PermissionRequestStatus.Pending)
        {
            throw new InvalidOperationException("只有待审批权限申请可以审批。");
        }

        // 审批单留痕（驳回）
        if (request.ReviewId is > 0)
        {
            await _reviewDomainService.AuditReviewAsync(
                new ReviewAuditCommand(
                    request.ReviewId.Value,
                    AuditResult.Reject,
                    command.OperatorUserId,
                    null,
                    command.Remark,
                    "Reject",
                    now,
                    null,
                    null,
                    command.Remark,
                    command.OperatorUserId),
                cancellationToken);
        }

        request.RequestStatus = PermissionRequestStatus.Rejected;
        request.Remark = NormalizeNullable(command.Remark) ?? request.Remark;
        var savedRequest = await _permissionRequestRepository.UpdateAsync(request, cancellationToken);
        return new PermissionRequestCommandResult(savedRequest.BasicId);
    }

    /// <inheritdoc />
    public async Task WithdrawPermissionRequestAsync(long id, long requestUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permissionRequest = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        EnsurePendingOwnerRequest(permissionRequest, requestUserId);

        permissionRequest.RequestStatus = PermissionRequestStatus.Withdrawn;
        _ = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PermissionRequestCommandResult> UpdatePermissionRequestAsync(PermissionRequestUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限申请主键必须大于 0。");
        }

        ValidateRequestCommonInput(
            command.PermissionId,
            command.RoleId,
            command.RequestReason,
            command.ExpectedEffectiveTime,
            command.ExpectedExpirationTime,
            now);

        var permissionRequest = await GetPermissionRequestOrThrowAsync(command.BasicId, cancellationToken);
        EnsurePendingOwnerRequest(permissionRequest, command.RequestUserId);

        _ = await GetAvailableTenantMemberForRequestOrThrowAsync(command.RequestUserId, now, cancellationToken);
        _ = await GetRequestablePermissionOrDefaultAsync(command.PermissionId, cancellationToken);
        _ = await GetRequestableRoleOrDefaultAsync(command.RoleId, cancellationToken);
        await EnsurePendingRequestNotExistsAsync(command.RequestUserId, command.PermissionId, command.RoleId, permissionRequest.BasicId, cancellationToken);

        permissionRequest.PermissionId = command.PermissionId;
        permissionRequest.RoleId = command.RoleId;
        permissionRequest.RequestReason = command.RequestReason.Trim();
        permissionRequest.ExpectedEffectiveTime = command.ExpectedEffectiveTime;
        permissionRequest.ExpectedExpirationTime = command.ExpectedExpirationTime;
        permissionRequest.Remark = NormalizeNullable(command.Remark);

        var savedRequest = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
        return new PermissionRequestCommandResult(savedRequest.BasicId);
    }

    /// <inheritdoc />
    public async Task<PermissionRequestCommandResult> UpdatePermissionRequestStatusAsync(PermissionRequestStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限申请主键必须大于 0。");
        }

        ValidateEnum(command.RequestStatus, nameof(command.RequestStatus));
        ValidateOptionalId(command.ReviewId, nameof(command.ReviewId), "审批单主键必须大于 0。");

        _ = await GetAvailableTenantMemberForRequestOrThrowAsync(command.OperatorUserId, DateTimeOffset.UtcNow, cancellationToken);
        var permissionRequest = await GetPermissionRequestOrThrowAsync(command.BasicId, cancellationToken);
        EnsureStatusCanBeChanged(permissionRequest, command.RequestStatus);

        if (command.ReviewId.HasValue)
        {
            _ = await GetEnabledReviewOrThrowAsync(command.ReviewId.Value, cancellationToken);
        }

        permissionRequest.RequestStatus = command.RequestStatus;
        permissionRequest.ReviewId = command.ReviewId ?? permissionRequest.ReviewId;
        permissionRequest.Remark = NormalizeNullable(command.Remark);

        var savedRequest = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
        return new PermissionRequestCommandResult(savedRequest.BasicId);
    }

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

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string BuildReviewTitle(long? permissionId, long? roleId)
    {
        if (roleId is > 0 && permissionId is > 0)
        {
            return "权限申请：角色 + 权限";
        }

        if (roleId is > 0)
        {
            return "权限申请：角色授予";
        }

        return "权限申请：权限授予";
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void ValidateOptionalId(long? id, string paramName, string message)
    {
        if (id.HasValue && id.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateRequestCommonInput(
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

    private async Task<SysTenantUser> GetAvailableTenantMemberForRequestOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
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

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台管理员成员权限申请仅平台运维态可维护，请切换到平台运维后操作。");
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

    private async Task<SysPermissionRequest> GetPermissionRequestOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限申请主键必须大于 0。");
        }

        return await _permissionRequestRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限申请不存在。");
    }

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

        if ((role.IsGlobal || role.RoleType == RoleType.System) && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台全局角色或系统角色权限申请仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return role;
    }
}
