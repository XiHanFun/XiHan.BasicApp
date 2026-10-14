// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
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
    private readonly IPermissionRepository _permissionRepository;
    private readonly IPermissionRequestRepository _permissionRequestRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICurrentTenant _currentTenant;

    private readonly IConstraintRuleEnforcementDomainService _constraintRuleEnforcementDomainService;

    private readonly ILogger<PermissionRequestDomainService> _logger;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionRequestDomainService(
        IPermissionRequestRepository permissionRequestRepository,
        ITenantUserRepository tenantUserRepository,
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserRoleRepository userRoleRepository,
        IUserPermissionRepository userPermissionRepository,
        ICurrentTenant currentTenant,
        IConstraintRuleEnforcementDomainService constraintRuleEnforcementDomainService,
        ILogger<PermissionRequestDomainService> logger)
    {
        _permissionRequestRepository = permissionRequestRepository;
        _tenantUserRepository = tenantUserRepository;
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _userPermissionRepository = userPermissionRepository;
        _currentTenant = currentTenant;
        _constraintRuleEnforcementDomainService = constraintRuleEnforcementDomainService;
        _logger = logger;
    }

    /// <summary>
    /// 创建权限申请
    /// </summary>
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
        return new PermissionRequestCommandResult(savedRequest.BasicId);
    }

    /// <summary>
    /// 关联审批流程实例
    /// </summary>
    public async Task AttachWorkflowInstanceAsync(long id, long workflowInstanceId, CancellationToken cancellationToken = default)
    {
        if (workflowInstanceId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workflowInstanceId), "流程实例主键必须大于 0。");
        }

        var request = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        request.WorkflowInstanceId = workflowInstanceId;
        _ = await _permissionRequestRepository.UpdateAsync(request, cancellationToken);
    }

    /// <summary>
    /// 回写审批通过：为申请人自动授予角色/权限并置为已批准。
    /// </summary>
    /// <remarks>
    /// 由权限申请流程的结果结束节点调用，与授权写入同一工作单元；SSD 违规或申请对象已停用时抛出，
    /// 流程停在故障态，修正后重试即重新执行。已批准时幂等返回、不重复授权。
    /// </remarks>
    public async Task<PermissionRequestCommandResult> ApplyApprovalAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        if (request.RequestStatus == PermissionRequestStatus.Approved)
        {
            return new PermissionRequestCommandResult(request.BasicId);
        }

        if (request.RequestStatus != PermissionRequestStatus.Pending)
        {
            throw new InvalidOperationException($"权限申请当前为 {request.RequestStatus}，不能按审批通过授权。");
        }

        // 重新校验申请对象仍可用（防止审批期间权限/角色已被停用）
        _ = await GetRequestablePermissionOrDefaultAsync(request.PermissionId, cancellationToken);
        _ = await GetRequestableRoleOrDefaultAsync(request.RoleId, cancellationToken);

        // 自动授权：为申请人创建角色 / 权限直授（沿用申请的期望有效期）
        var grantReason = $"权限申请[{request.BasicId}]审批通过自动授权";
        if (request.RoleId is > 0)
        {
            // SSD 执法：审批自动授予角色与手动授予同一约束语义，违规时阻断授权（申请保持待审批，流程停在故障态可重试）。
            await EnsureNoSoDConflictOnApprovalAsync(request.RequestUserId, request.RoleId.Value, cancellationToken);

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

        request.RequestStatus = PermissionRequestStatus.Approved;
        var savedRequest = await _permissionRequestRepository.UpdateAsync(request, cancellationToken);
        return new PermissionRequestCommandResult(
            savedRequest.BasicId,
            request.RequestUserId,
            request.RoleId is > 0 ? request.RoleId : null,
            request.PermissionId is > 0 ? request.PermissionId : null);
    }

    /// <summary>
    /// 回写审批驳回：置为已驳回（已驳回时幂等返回）。
    /// </summary>
    public async Task<PermissionRequestCommandResult> ApplyRejectionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        if (request.RequestStatus == PermissionRequestStatus.Rejected)
        {
            return new PermissionRequestCommandResult(request.BasicId);
        }

        if (request.RequestStatus != PermissionRequestStatus.Pending)
        {
            throw new InvalidOperationException($"权限申请当前为 {request.RequestStatus}，不能按审批驳回处理。");
        }

        request.RequestStatus = PermissionRequestStatus.Rejected;
        var savedRequest = await _permissionRequestRepository.UpdateAsync(request, cancellationToken);
        return new PermissionRequestCommandResult(savedRequest.BasicId);
    }

    /// <summary>
    /// 回写审批中止：待审批的申请置为已撤回，已完结的申请保持不变。
    /// </summary>
    public async Task MarkAbortedAsync(long id, string? reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        if (request.RequestStatus != PermissionRequestStatus.Pending)
        {
            return;
        }

        request.RequestStatus = PermissionRequestStatus.Withdrawn;
        request.Remark = NormalizeNullable(reason) ?? request.Remark;
        _ = await _permissionRequestRepository.UpdateAsync(request, cancellationToken);
    }

    /// <summary>
    /// 撤回权限申请
    /// </summary>
    public async Task WithdrawPermissionRequestAsync(long id, long requestUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var permissionRequest = await GetPermissionRequestOrThrowAsync(id, cancellationToken);
        EnsurePendingOwnerRequest(permissionRequest, requestUserId);

        permissionRequest.RequestStatus = PermissionRequestStatus.Withdrawn;
        _ = await _permissionRequestRepository.UpdateAsync(permissionRequest, cancellationToken);
    }

    /// <summary>
    /// 更新权限申请
    /// </summary>
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

    /// <summary>
    /// 更新权限申请状态
    /// </summary>
    public async Task<PermissionRequestCommandResult> UpdatePermissionRequestStatusAsync(PermissionRequestStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限申请主键必须大于 0。");
        }

        ValidateEnum(command.RequestStatus, nameof(command.RequestStatus));

        _ = await GetAvailableTenantMemberForRequestOrThrowAsync(command.OperatorUserId, DateTimeOffset.UtcNow, cancellationToken);
        var permissionRequest = await GetPermissionRequestOrThrowAsync(command.BasicId, cancellationToken);
        EnsureStatusCanBeChanged(permissionRequest, command.RequestStatus);

        permissionRequest.RequestStatus = command.RequestStatus;
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
            throw new InvalidOperationException("权限申请审批通过必须走审批流程并自动授权，不能直接更新为已批准。");
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

    /// <summary>
    /// 审批自动授权前的静态职责分离（SSD）执法：违规阻断审批，申请保持待审批状态可重试。
    /// </summary>
    private async Task EnsureNoSoDConflictOnApprovalAsync(long userId, long roleId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var existingRoleIds = (await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken))
            .Select(userRole => userRole.RoleId);

        var result = await _constraintRuleEnforcementDomainService.EvaluateRoleAssignmentsAsync(
            existingRoleIds.Append(roleId),
            ConstraintType.SSD,
            cancellationToken);

        var blocking = result.FirstBlockingViolation;
        if (blocking is not null)
        {
            throw new InvalidOperationException(
                $"权限申请审批通过将违反职责分离约束规则[{blocking.RuleCode}]《{blocking.RuleName}》（冲突角色主键：{string.Join(",", blocking.MatchedTargetIds)}）。");
        }

        foreach (var violation in result.Violations)
        {
            _logger.LogWarning(
                "权限申请自动授权命中职责分离约束规则[{RuleCode}]《{RuleName}》，按 {ViolationAction} 处理放行。",
                violation.RuleCode,
                violation.RuleName,
                violation.ViolationAction);
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
