#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PermissionDelegationAppService
// Guid:0e0665f8-72bd-4ee9-90fb-daf57fe17c39
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
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 权限委托命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "权限委托")]
public sealed class PermissionDelegationAppService(
    IPermissionDelegationRepository permissionDelegationRepository,
    ITenantUserRepository tenantUserRepository,
    IPermissionRepository permissionRepository,
    IRoleRepository roleRepository)
    : SaasApplicationService, IPermissionDelegationAppService
{
    /// <summary>
    /// 权限委托仓储
    /// </summary>
    private readonly IPermissionDelegationRepository _permissionDelegationRepository = permissionDelegationRepository;

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
    /// 创建权限委托
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Create)]
    public async Task<PermissionDelegationDetailDto> CreatePermissionDelegationAsync(PermissionDelegationCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateCreateInput(input, now);

        var delegator = await GetAvailableTenantMemberOrThrowAsync(input.DelegatorUserId, now, "委托人", cancellationToken);
        var delegatee = await GetAvailableTenantMemberOrThrowAsync(input.DelegateeUserId, now, "被委托人", cancellationToken);
        var permission = await GetDelegablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetDelegableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsureDelegationNotExistsAsync(input.DelegatorUserId, input.DelegateeUserId, input.PermissionId, null, cancellationToken);

        var delegation = new SysPermissionDelegation
        {
            DelegatorUserId = input.DelegatorUserId,
            DelegateeUserId = input.DelegateeUserId,
            PermissionId = input.PermissionId,
            RoleId = input.RoleId,
            DelegationStatus = ResolveWritableStatus(input.EffectiveTime, now),
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            DelegationReason = NormalizeNullable(input.DelegationReason),
            Remark = NormalizeNullable(input.Remark)
        };

        var savedDelegation = await _permissionDelegationRepository.AddAsync(delegation, cancellationToken);
        return PermissionDelegationApplicationMapper.ToDetailDto(savedDelegation, delegator, delegatee, permission, role, now);
    }

    /// <summary>
    /// 更新权限委托
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Update)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationAsync(PermissionDelegationUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        ValidateUpdateInput(input, now);

        var delegation = await GetPermissionDelegationOrThrowAsync(input.BasicId, cancellationToken);
        if (delegation.DelegationStatus == DelegationStatus.Revoked)
        {
            throw new InvalidOperationException("已撤销权限委托不能更新。");
        }

        var delegator = await GetAvailableTenantMemberOrThrowAsync(input.DelegatorUserId, now, "委托人", cancellationToken);
        var delegatee = await GetAvailableTenantMemberOrThrowAsync(input.DelegateeUserId, now, "被委托人", cancellationToken);
        var permission = await GetDelegablePermissionOrDefaultAsync(input.PermissionId, cancellationToken);
        var role = await GetDelegableRoleOrDefaultAsync(input.RoleId, cancellationToken);
        await EnsureDelegationNotExistsAsync(input.DelegatorUserId, input.DelegateeUserId, input.PermissionId, delegation.BasicId, cancellationToken);

        delegation.DelegatorUserId = input.DelegatorUserId;
        delegation.DelegateeUserId = input.DelegateeUserId;
        delegation.PermissionId = input.PermissionId;
        delegation.RoleId = input.RoleId;
        delegation.DelegationStatus = ResolveWritableStatus(input.EffectiveTime, now);
        delegation.EffectiveTime = input.EffectiveTime;
        delegation.ExpirationTime = input.ExpirationTime;
        delegation.DelegationReason = NormalizeNullable(input.DelegationReason);
        delegation.Remark = NormalizeNullable(input.Remark);

        var savedDelegation = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
        return PermissionDelegationApplicationMapper.ToDetailDto(savedDelegation, delegator, delegatee, permission, role, now);
    }

    /// <summary>
    /// 更新权限委托状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Status)]
    public async Task<PermissionDelegationDetailDto> UpdatePermissionDelegationStatusAsync(PermissionDelegationStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限委托主键必须大于 0。");
        }

        ValidateEnum(input.DelegationStatus, nameof(input.DelegationStatus));

        var now = DateTimeOffset.UtcNow;
        var delegation = await GetPermissionDelegationOrThrowAsync(input.BasicId, cancellationToken);
        if (delegation.DelegationStatus == DelegationStatus.Revoked && input.DelegationStatus != DelegationStatus.Revoked)
        {
            throw new InvalidOperationException("已撤销权限委托不能重新生效。");
        }

        ValidateStatusMatchesPeriod(input.DelegationStatus, delegation.EffectiveTime, delegation.ExpirationTime, now);

        SysTenantUser? delegator;
        SysTenantUser? delegatee;
        SysPermission? permission;
        SysRole? role;
        if (input.DelegationStatus is DelegationStatus.Pending or DelegationStatus.Active)
        {
            delegator = await GetAvailableTenantMemberOrThrowAsync(delegation.DelegatorUserId, now, "委托人", cancellationToken);
            delegatee = await GetAvailableTenantMemberOrThrowAsync(delegation.DelegateeUserId, now, "被委托人", cancellationToken);
            permission = await GetDelegablePermissionOrDefaultAsync(delegation.PermissionId, cancellationToken);
            role = await GetDelegableRoleOrDefaultAsync(delegation.RoleId, cancellationToken);
        }
        else
        {
            delegator = await _tenantUserRepository.GetMembershipAsync(delegation.DelegatorUserId, cancellationToken);
            delegatee = await _tenantUserRepository.GetMembershipAsync(delegation.DelegateeUserId, cancellationToken);
            permission = await GetPermissionOrDefaultAsync(delegation.PermissionId, cancellationToken);
            role = await GetRoleOrDefaultAsync(delegation.RoleId, cancellationToken);
        }

        delegation.DelegationStatus = input.DelegationStatus;
        delegation.Remark = NormalizeNullable(input.Remark);

        var savedDelegation = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
        return PermissionDelegationApplicationMapper.ToDetailDto(savedDelegation, delegator, delegatee, permission, role, now);
    }

    /// <summary>
    /// 撤销权限委托
    /// </summary>
    /// <param name="id">权限委托主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.PermissionDelegation.Revoke)]
    public async Task DeletePermissionDelegationAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var delegation = await GetPermissionDelegationOrThrowAsync(id, cancellationToken);
        delegation.DelegationStatus = DelegationStatus.Revoked;

        _ = await _permissionDelegationRepository.UpdateAsync(delegation, cancellationToken);
    }

    /// <summary>
    /// 获取权限委托，不存在时抛出异常
    /// </summary>
    /// <param name="id">权限委托主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限委托实体</returns>
    private async Task<SysPermissionDelegation> GetPermissionDelegationOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "权限委托主键必须大于 0。");
        }

        return await _permissionDelegationRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("权限委托不存在。");
    }

    /// <summary>
    /// 获取可参与委托的租户成员，不满足规则时抛出异常
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="now">当前时间</param>
    /// <param name="subjectName">主体名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员实体</returns>
    private async Task<SysTenantUser> GetAvailableTenantMemberOrThrowAsync(long userId, DateTimeOffset now, string subjectName, CancellationToken cancellationToken)
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

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员权限委托必须通过平台运维流程维护。");
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

    /// <summary>
    /// 获取可委托权限
    /// </summary>
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

    /// <summary>
    /// 获取可委托角色
    /// </summary>
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

        if (role.IsGlobal || role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("平台全局角色或系统角色权限委托必须通过平台运维流程维护。");
        }

        return role;
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
    /// 校验权限委托不存在
    /// </summary>
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

    /// <summary>
    /// 校验创建参数
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="now">当前时间</param>
    private static void ValidateCreateInput(PermissionDelegationCreateDto input, DateTimeOffset now)
    {
        ValidateCommonInput(
            input.DelegatorUserId,
            input.DelegateeUserId,
            input.PermissionId,
            input.RoleId,
            input.EffectiveTime,
            input.ExpirationTime,
            now);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="now">当前时间</param>
    private static void ValidateUpdateInput(PermissionDelegationUpdateDto input, DateTimeOffset now)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限委托主键必须大于 0。");
        }

        ValidateCommonInput(
            input.DelegatorUserId,
            input.DelegateeUserId,
            input.PermissionId,
            input.RoleId,
            input.EffectiveTime,
            input.ExpirationTime,
            now);
    }

    /// <summary>
    /// 校验通用参数
    /// </summary>
    private static void ValidateCommonInput(
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

    /// <summary>
    /// 校验可写有效期
    /// </summary>
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

    /// <summary>
    /// 校验状态与有效期一致
    /// </summary>
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
    /// 解析可写状态
    /// </summary>
    private static DelegationStatus ResolveWritableStatus(DateTimeOffset? effectiveTime, DateTimeOffset now)
    {
        return effectiveTime.HasValue && effectiveTime.Value > now
            ? DelegationStatus.Pending
            : DelegationStatus.Active;
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
