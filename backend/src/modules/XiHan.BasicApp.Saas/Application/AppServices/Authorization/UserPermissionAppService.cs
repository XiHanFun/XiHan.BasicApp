#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserPermissionAppService
// Guid:8b8a1425-e889-48e9-8f03-2388b7c50ce3
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
/// 用户直授权限命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户直授权限")]
public sealed class UserPermissionAppService(
    IUserPermissionRepository userPermissionRepository,
    IPermissionRepository permissionRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserPermissionAppService
{
    /// <summary>
    /// 用户直授权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 授予用户直授权限
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Grant)]
    public async Task<UserPermissionDetailDto> CreateUserPermissionAsync(UserPermissionGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantInput(input);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, cancellationToken);
        var permission = await GetGrantablePermissionOrThrowAsync(input.PermissionId, cancellationToken);
        if (await _userPermissionRepository.AnyAsync(
            userPermission => userPermission.UserId == input.UserId && userPermission.PermissionId == input.PermissionId,
            cancellationToken))
        {
            throw new InvalidOperationException("用户直授权限已绑定。");
        }

        var userPermission = new SysUserPermission
        {
            UserId = input.UserId,
            PermissionId = input.PermissionId,
            PermissionAction = input.PermissionAction,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            GrantReason = NormalizeNullable(input.GrantReason),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedUserPermission = await _userPermissionRepository.AddAsync(userPermission, cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(savedUserPermission, permission, tenantMember, now);
    }

    /// <summary>
    /// 更新用户直授权限
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Update)]
    public async Task<UserPermissionDetailDto> UpdateUserPermissionAsync(UserPermissionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var userPermission = await GetUserPermissionOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, cancellationToken);
        var permission = await GetGrantablePermissionOrThrowAsync(userPermission.PermissionId, cancellationToken);

        userPermission.PermissionAction = input.PermissionAction;
        userPermission.EffectiveTime = input.EffectiveTime;
        userPermission.ExpirationTime = input.ExpirationTime;
        userPermission.GrantReason = NormalizeNullable(input.GrantReason);
        userPermission.Remark = NormalizeNullable(input.Remark);

        var savedUserPermission = await _userPermissionRepository.UpdateAsync(userPermission, cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(savedUserPermission, permission, tenantMember, now);
    }

    /// <summary>
    /// 更新用户直授权限状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Status)]
    public async Task<UserPermissionDetailDto> UpdateUserPermissionStatusAsync(UserPermissionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户直授权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var now = DateTimeOffset.UtcNow;
        var userPermission = await GetUserPermissionOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = input.Status == ValidityStatus.Valid
            ? await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);
        var permission = input.Status == ValidityStatus.Valid
            ? await GetGrantablePermissionOrThrowAsync(userPermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);

        userPermission.Status = input.Status;
        userPermission.Remark = NormalizeNullable(input.Remark);

        var savedUserPermission = await _userPermissionRepository.UpdateAsync(userPermission, cancellationToken);
        return UserPermissionApplicationMapper.ToDetailDto(savedUserPermission, permission, tenantMember, now);
    }

    /// <summary>
    /// 撤销用户直授权限
    /// </summary>
    /// <param name="id">用户直授权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserPermission.Revoke)]
    public async Task DeleteUserPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userPermission = await GetUserPermissionOrThrowAsync(id, cancellationToken);
        userPermission.Status = ValidityStatus.Invalid;

        _ = await _userPermissionRepository.UpdateAsync(userPermission, cancellationToken);
    }

    /// <summary>
    /// 获取用户直授权限绑定，不存在时抛出异常
    /// </summary>
    /// <param name="id">用户直授权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限绑定实体</returns>
    private async Task<SysUserPermission> GetUserPermissionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户直授权限绑定主键必须大于 0。");
        }

        return await _userPermissionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户直授权限绑定不存在。");
    }

    /// <summary>
    /// 获取可授权租户成员，不满足规则时抛出异常
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="now">当前时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户成员实体</returns>
    private async Task<SysTenantUser> GetAssignableTenantMemberOrThrowAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能维护直授权限。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能维护直授权限。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员权限必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能维护直授权限。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能维护直授权限。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 获取可授权权限，不满足规则时抛出异常
    /// </summary>
    /// <param name="permissionId">权限主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>权限实体</returns>
    private async Task<SysPermission> GetGrantablePermissionOrThrowAsync(long permissionId, CancellationToken cancellationToken)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, cancellationToken)
            ?? throw new InvalidOperationException("权限不存在。");

        if (permission.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用权限不能直授给用户。");
        }

        return permission;
    }

    /// <summary>
    /// 校验授权参数
    /// </summary>
    /// <param name="input">授权参数</param>
    private static void ValidateGrantInput(UserPermissionGrantDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "权限主键必须大于 0。");
        }

        ValidateEnum(input.PermissionAction, nameof(input.PermissionAction));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(UserPermissionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户直授权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.PermissionAction, nameof(input.PermissionAction));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验有效期
    /// </summary>
    /// <param name="effectiveTime">生效时间</param>
    /// <param name="expirationTime">失效时间</param>
    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException("用户直授权限失效时间必须晚于生效时间。");
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
