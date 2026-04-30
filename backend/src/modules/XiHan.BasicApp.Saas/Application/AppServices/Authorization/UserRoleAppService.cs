#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserRoleAppService
// Guid:e6517b47-fc4d-4bfd-bb53-fe3ce8ee75d5
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
/// 用户角色命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户角色")]
public sealed class UserRoleAppService(
    IUserRoleRepository userRoleRepository,
    IRoleRepository roleRepository,
    ITenantUserRepository tenantUserRepository)
    : SaasApplicationService, IUserRoleAppService
{
    /// <summary>
    /// 用户角色仓储
    /// </summary>
    private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 授予用户角色
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Grant)]
    public async Task<UserRoleDetailDto> CreateUserRoleAsync(UserRoleGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateGrantInput(input);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, cancellationToken);
        var role = await GetAssignableRoleOrThrowAsync(input.RoleId, cancellationToken);
        if (await _userRoleRepository.AnyAsync(
            userRole => userRole.UserId == input.UserId && userRole.RoleId == input.RoleId,
            cancellationToken))
        {
            throw new InvalidOperationException("用户角色已绑定。");
        }

        var userRole = new SysUserRole
        {
            UserId = input.UserId,
            RoleId = input.RoleId,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            GrantReason = NormalizeNullable(input.GrantReason),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedUserRole = await _userRoleRepository.AddAsync(userRole, cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(savedUserRole, role, tenantMember, now);
    }

    /// <summary>
    /// 更新用户角色
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Update)]
    public async Task<UserRoleDetailDto> UpdateUserRoleAsync(UserRoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var userRole = await GetUserRoleOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, cancellationToken);
        var role = await GetAssignableRoleOrThrowAsync(userRole.RoleId, cancellationToken);

        userRole.EffectiveTime = input.EffectiveTime;
        userRole.ExpirationTime = input.ExpirationTime;
        userRole.GrantReason = NormalizeNullable(input.GrantReason);
        userRole.Remark = NormalizeNullable(input.Remark);

        var savedUserRole = await _userRoleRepository.UpdateAsync(userRole, cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(savedUserRole, role, tenantMember, now);
    }

    /// <summary>
    /// 更新用户角色状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Status)]
    public async Task<UserRoleDetailDto> UpdateUserRoleStatusAsync(UserRoleStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户角色绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var now = DateTimeOffset.UtcNow;
        var userRole = await GetUserRoleOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = input.Status == ValidityStatus.Valid
            ? await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(userRole.UserId, cancellationToken);
        var role = input.Status == ValidityStatus.Valid
            ? await GetAssignableRoleOrThrowAsync(userRole.RoleId, cancellationToken)
            : await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);

        userRole.Status = input.Status;
        userRole.Remark = NormalizeNullable(input.Remark);

        var savedUserRole = await _userRoleRepository.UpdateAsync(userRole, cancellationToken);
        return UserRoleApplicationMapper.ToDetailDto(savedUserRole, role, tenantMember, now);
    }

    /// <summary>
    /// 撤销用户角色
    /// </summary>
    /// <param name="id">用户角色绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserRole.Revoke)]
    public async Task DeleteUserRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userRole = await GetUserRoleOrThrowAsync(id, cancellationToken);
        userRole.Status = ValidityStatus.Invalid;

        _ = await _userRoleRepository.UpdateAsync(userRole, cancellationToken);
    }

    /// <summary>
    /// 获取用户角色绑定，不存在时抛出异常
    /// </summary>
    /// <param name="id">用户角色绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色绑定实体</returns>
    private async Task<SysUserRole> GetUserRoleOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户角色绑定主键必须大于 0。");
        }

        return await _userRoleRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户角色绑定不存在。");
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
            throw new InvalidOperationException("未接受邀请的租户成员不能分配角色。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能分配角色。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("平台管理员成员角色必须通过平台运维流程维护。");
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能分配角色。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能分配角色。");
        }

        return tenantMember;
    }

    /// <summary>
    /// 获取可授权角色，不满足规则时抛出异常
    /// </summary>
    /// <param name="roleId">角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色实体</returns>
    private async Task<SysRole> GetAssignableRoleOrThrowAsync(long roleId, CancellationToken cancellationToken)
    {
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能分配给用户。");
        }

        if (role.RoleType == RoleType.System)
        {
            throw new InvalidOperationException("系统角色必须通过平台运维流程分配。");
        }

        return role;
    }

    /// <summary>
    /// 校验授权参数
    /// </summary>
    /// <param name="input">授权参数</param>
    private static void ValidateGrantInput(UserRoleGrantDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色主键必须大于 0。");
        }

        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    /// <param name="input">更新参数</param>
    private static void ValidateUpdateInput(UserRoleUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户角色绑定主键必须大于 0。");
        }

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
            throw new InvalidOperationException("用户角色失效时间必须晚于生效时间。");
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
