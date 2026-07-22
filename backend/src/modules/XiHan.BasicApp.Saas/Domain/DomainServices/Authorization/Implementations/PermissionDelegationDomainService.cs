// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 权限委托领域服务实现
/// </summary>
public sealed class PermissionDelegationDomainService
    : IPermissionDelegationDomainService
{
    private readonly IPermissionDelegationRepository _permissionDelegationRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ITenantUserRepository _tenantUserRepository;
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PermissionDelegationDomainService(
        IPermissionDelegationRepository permissionDelegationRepository,
        ITenantUserRepository tenantUserRepository,
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        ICurrentTenant currentTenant)
    {
        _permissionDelegationRepository = permissionDelegationRepository;
        _tenantUserRepository = tenantUserRepository;
        _permissionRepository = permissionRepository;
        _roleRepository = roleRepository;
        _currentTenant = currentTenant;
    }

    /// <inheritdoc />
    public async Task<PermissionDelegationCommandResult> CreatePermissionDelegationAsync(PermissionDelegationCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateDelegationCommonInput(
            command.DelegatorUserId,
            command.DelegateeUserId,
            command.PermissionId,
            command.RoleId,
            command.EffectiveTime,
            command.ExpirationTime,
            now);

        _ = await GetAvailableTenantMemberWithSubjectOrThrowAsync(command.DelegatorUserId, now, "委托人", cancellationToken);
        _ = await GetAvailableTenantMemberWithSubjectOrThrowAsync(command.DelegateeUserId, now, "被委托人", cancellationToken);
        _ = await GetDelegablePermissionOrDefaultAsync(command.PermissionId, cancellationToken);
        _ = await GetDelegableRoleOrDefaultAsync(command.RoleId, cancellationToken);
        await EnsureDelegationNotExistsAsync(command.DelegatorUserId, command.DelegateeUserId, command.PermissionId, null, cancellationToken);

        var delegation = new SysPermissionDelegation
        {
            DelegatorUserId = command.DelegatorUserId,
            DelegateeUserId = command.DelegateeUserId,
            PermissionId = command.PermissionId,
            RoleId = command.RoleId,
            DelegationStatus = ResolveWritableStatus(command.EffectiveTime, now),
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            DelegationReason = NormalizeNullable(command.DelegationReason),
            Remark = NormalizeNullable(command.Remark)
        };

        var savedDelegation = await _permissionDelegationRepository.AddAsync(delegation, cancellationToken);
        return ToCommandResult(savedDelegation);
    }

    /// <inheritdoc />
    public async Task<PermissionDelegationCommandResult> RevokePermissionDelegationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var delegation = await GetPermissionDelegationOrThrowAsync(id, cancellationToken);
        delegation.DelegationStatus = DelegationStatus.Revoked;

        var savedDelegation = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
        return ToCommandResult(savedDelegation);
    }

    /// <inheritdoc />
    public async Task<PermissionDelegationCommandResult> UpdatePermissionDelegationAsync(PermissionDelegationUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限委托主键必须大于 0。");
        }

        ValidateDelegationCommonInput(
            command.DelegatorUserId,
            command.DelegateeUserId,
            command.PermissionId,
            command.RoleId,
            command.EffectiveTime,
            command.ExpirationTime,
            now);

        var delegation = await GetPermissionDelegationOrThrowAsync(command.BasicId, cancellationToken);
        if (delegation.DelegationStatus == DelegationStatus.Revoked)
        {
            throw new InvalidOperationException("已撤销权限委托不能更新。");
        }

        _ = await GetAvailableTenantMemberWithSubjectOrThrowAsync(command.DelegatorUserId, now, "委托人", cancellationToken);
        _ = await GetAvailableTenantMemberWithSubjectOrThrowAsync(command.DelegateeUserId, now, "被委托人", cancellationToken);
        _ = await GetDelegablePermissionOrDefaultAsync(command.PermissionId, cancellationToken);
        _ = await GetDelegableRoleOrDefaultAsync(command.RoleId, cancellationToken);
        await EnsureDelegationNotExistsAsync(command.DelegatorUserId, command.DelegateeUserId, command.PermissionId, delegation.BasicId, cancellationToken);

        delegation.DelegatorUserId = command.DelegatorUserId;
        delegation.DelegateeUserId = command.DelegateeUserId;
        delegation.PermissionId = command.PermissionId;
        delegation.RoleId = command.RoleId;
        delegation.DelegationStatus = ResolveWritableStatus(command.EffectiveTime, now);
        delegation.EffectiveTime = command.EffectiveTime;
        delegation.ExpirationTime = command.ExpirationTime;
        delegation.DelegationReason = NormalizeNullable(command.DelegationReason);
        delegation.Remark = NormalizeNullable(command.Remark);

        var savedDelegation = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
        return ToCommandResult(savedDelegation);
    }

    /// <inheritdoc />
    public async Task<PermissionDelegationCommandResult> UpdatePermissionDelegationStatusAsync(PermissionDelegationStatusCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限委托主键必须大于 0。");
        }

        ValidateEnum(command.DelegationStatus, nameof(command.DelegationStatus));

        var now = DateTimeOffset.UtcNow;
        var delegation = await GetPermissionDelegationOrThrowAsync(command.BasicId, cancellationToken);
        if (delegation.DelegationStatus == DelegationStatus.Revoked && command.DelegationStatus != DelegationStatus.Revoked)
        {
            throw new InvalidOperationException("已撤销权限委托不能重新生效。");
        }

        ValidateStatusMatchesPeriod(command.DelegationStatus, delegation.EffectiveTime, delegation.ExpirationTime, now);
        if (command.DelegationStatus is DelegationStatus.Pending or DelegationStatus.Active)
        {
            _ = await GetAvailableTenantMemberWithSubjectOrThrowAsync(delegation.DelegatorUserId, now, "委托人", cancellationToken);
            _ = await GetAvailableTenantMemberWithSubjectOrThrowAsync(delegation.DelegateeUserId, now, "被委托人", cancellationToken);
            _ = await GetDelegablePermissionOrDefaultAsync(delegation.PermissionId, cancellationToken);
            _ = await GetDelegableRoleOrDefaultAsync(delegation.RoleId, cancellationToken);
        }

        delegation.DelegationStatus = command.DelegationStatus;
        delegation.Remark = NormalizeNullable(command.Remark);

        var savedDelegation = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
        return ToCommandResult(savedDelegation);
    }

    private static PermissionDelegationCommandResult ToCommandResult(SysPermissionDelegation delegation)
    {
        // 生效/待生效视为授予；已撤销/已过期视为收回
        var isActive = delegation.DelegationStatus is DelegationStatus.Active or DelegationStatus.Pending;
        return new PermissionDelegationCommandResult(
            delegation.BasicId,
            delegation.DelegateeUserId,
            delegation.PermissionId,
            delegation.RoleId,
            isActive);
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static DelegationStatus ResolveWritableStatus(DateTimeOffset? effectiveTime, DateTimeOffset now)
    {
        return effectiveTime.HasValue && effectiveTime.Value > now
            ? DelegationStatus.Pending
            : DelegationStatus.Active;
    }

    private static void ValidateDelegationCommonInput(
        long delegatorUserId,
        long delegateeUserId,
        long? permissionId,
        long? roleId,
        DateTimeOffset? effectiveTime,
        DateTimeOffset expirationTime,
        DateTimeOffset now)
    {
        if (delegatorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delegatorUserId), "委托人用户主键必须大于 0。");
        }

        if (delegateeUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(delegateeUserId), "被委托人用户主键必须大于 0。");
        }

        if (delegatorUserId == delegateeUserId)
        {
            throw new InvalidOperationException("委托人和被委托人不能相同。");
        }

        ValidateOptionalId(permissionId, nameof(permissionId), "权限主键必须大于 0。");
        ValidateOptionalId(roleId, nameof(roleId), "角色主键必须大于 0。");
        ValidateWritablePeriod(effectiveTime, expirationTime, now);
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

    private static void ValidateStatusMatchesPeriod(DelegationStatus status, DateTimeOffset? effectiveTime, DateTimeOffset expirationTime, DateTimeOffset now)
    {
        if (status == DelegationStatus.Pending && (!effectiveTime.HasValue || effectiveTime.Value <= now))
        {
            throw new InvalidOperationException("待生效权限委托必须存在晚于当前时间的生效时间。");
        }

        if (status == DelegationStatus.Active && ((effectiveTime.HasValue && effectiveTime.Value > now) || expirationTime <= now))
        {
            throw new InvalidOperationException("生效中权限委托必须处于当前有效期内。");
        }

        if (status == DelegationStatus.Expired && expirationTime > now)
        {
            throw new InvalidOperationException("未到失效时间的权限委托不能标记为已过期。");
        }
    }

    private static void ValidateWritablePeriod(DateTimeOffset? effectiveTime, DateTimeOffset expirationTime, DateTimeOffset now)
    {
        if (expirationTime == default)
        {
            throw new ArgumentOutOfRangeException(nameof(expirationTime), "权限委托失效时间不能为空。");
        }

        if (expirationTime <= now)
        {
            throw new InvalidOperationException("权限委托失效时间必须晚于当前时间。");
        }

        if (effectiveTime.HasValue && expirationTime <= effectiveTime.Value)
        {
            throw new InvalidOperationException("权限委托失效时间必须晚于生效时间。");
        }
    }

    private async Task EnsureDelegationNotExistsAsync(
        long delegatorUserId,
        long delegateeUserId,
        long? permissionId,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        var exists = excludeId.HasValue
            ? await _permissionDelegationRepository.AnyAsync(
                delegation => delegation.DelegatorUserId == delegatorUserId
                    && delegation.DelegateeUserId == delegateeUserId
                    && delegation.PermissionId == permissionId
                    && delegation.BasicId != excludeId.Value,
                cancellationToken)
            : await _permissionDelegationRepository.AnyAsync(
                delegation => delegation.DelegatorUserId == delegatorUserId
                    && delegation.DelegateeUserId == delegateeUserId
                    && delegation.PermissionId == permissionId,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("权限委托已存在。");
        }
    }

    private async Task<SysPermission?> GetDelegablePermissionOrDefaultAsync(long? permissionId, CancellationToken cancellationToken)
    {
        if (!permissionId.HasValue)
        {
            return null;
        }

        var permission = await _permissionRepository.GetByIdAsync(permissionId.Value, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能参与权限委托。");
        }

        return permission;
    }

    private async Task<SysRole?> GetDelegableRoleOrDefaultAsync(long? roleId, CancellationToken cancellationToken)
    {
        if (!roleId.HasValue)
        {
            return null;
        }

        var role = await _roleRepository.GetByIdAsync(roleId.Value, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能参与权限委托。");
        }

        if ((role.IsGlobal || role.RoleType == RoleType.System) && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台全局角色或系统角色权限委托仅平台运维态可维护，请切换到平台运维后操作。");
        }

        return role;
    }

    private async Task<SysTenantUser> GetAvailableTenantMemberWithSubjectOrThrowAsync(long userId, DateTimeOffset now, string subjectName, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException($"{subjectName}不是当前租户成员。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException($"未接受邀请的{subjectName}不能参与权限委托。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException($"无效{subjectName}不能参与权限委托。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("平台管理员成员权限委托仅平台运维态可维护，请切换到平台运维后操作。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException($"未生效{subjectName}不能参与权限委托。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException($"已过期{subjectName}不能参与权限委托。");
        }

        return tenantMember;
    }

    private async Task<SysPermissionDelegation> GetPermissionDelegationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限委托主键必须大于 0。");
        }

        return await _permissionDelegationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }
}
