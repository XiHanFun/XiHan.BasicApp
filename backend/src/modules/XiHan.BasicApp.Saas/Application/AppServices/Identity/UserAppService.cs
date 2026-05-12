#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserAppService
// Guid:ce9953f4-f7d4-4ebe-96cc-e282947f11ab
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Password;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户命令应用服务
/// 合并自：UserSecurityAppService / UserRoleAppService / UserPermissionAppService /
///         UserDataScopeAppService / UserDepartmentAppService / UserSessionAppService
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户")]
public sealed class UserAppService(
    IUserRepository userRepository,
    IUserSecurityRepository userSecurityRepository,
    ITenantUserRepository tenantUserRepository,
    IPasswordHasher passwordHasher,
    IAuthenticationService authenticationService,
    ICurrentUser currentUser,
    IUserRoleRepository userRoleRepository,
    IRoleRepository roleRepository,
    IUserPermissionRepository userPermissionRepository,
    IPermissionRepository permissionRepository,
    IUserDataScopeRepository userDataScopeRepository,
    IDepartmentRepository departmentRepository,
    IUserDepartmentRepository userDepartmentRepository,
    IUserSessionRepository userSessionRepository,
    ILocalEventBus localEventBus)
    : SaasApplicationService, IUserAppService
{
    // ================================================================
    // 字段 — 用户核心
    // ================================================================

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 用户安全仓储
    /// </summary>
    private readonly IUserSecurityRepository _userSecurityRepository = userSecurityRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 密码哈希服务
    /// </summary>
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    /// <summary>
    /// 认证服务
    /// </summary>
    private readonly IAuthenticationService _authenticationService = authenticationService;

    /// <summary>
    /// 当前用户
    /// </summary>
    private readonly ICurrentUser _currentUser = currentUser;

    // ================================================================
    // 字段 — 用户角色
    // ================================================================

    /// <summary>
    /// 用户角色仓储
    /// </summary>
    private readonly IUserRoleRepository _userRoleRepository = userRoleRepository;

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository = roleRepository;

    // ================================================================
    // 字段 — 用户直授权限
    // ================================================================

    /// <summary>
    /// 用户直授权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository = userPermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository = permissionRepository;

    // ================================================================
    // 字段 — 用户数据范围
    // ================================================================

    /// <summary>
    /// 用户数据范围仓储
    /// </summary>
    private readonly IUserDataScopeRepository _userDataScopeRepository = userDataScopeRepository;

    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    // ================================================================
    // 字段 — 用户部门归属
    // ================================================================

    /// <summary>
    /// 用户部门仓储
    /// </summary>
    private readonly IUserDepartmentRepository _userDepartmentRepository = userDepartmentRepository;

    // ================================================================
    // 字段 — 用户会话
    // ================================================================

    /// <summary>
    /// 用户会话仓储
    /// </summary>
    private readonly IUserSessionRepository _userSessionRepository = userSessionRepository;

    /// <summary>
    /// 本地事件总线
    /// </summary>
    private readonly ILocalEventBus _localEventBus = localEventBus;

    // ================================================================
    // #region 用户核心 (原始 UserAppService 方法)
    // ================================================================

    #region 用户核心

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Create)]
    public async Task<UserDetailDto> CreateUserAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var userName = input.UserName.Trim();
        if (await _userRepository.ExistsUserNameAsync(userName, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("用户名已存在。");
        }

        await EnsurePasswordMeetsPolicyAsync(input, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var user = new SysUser
        {
            UserName = userName,
            RealName = NormalizeNullable(input.RealName),
            NickName = NormalizeNullable(input.NickName),
            Avatar = NormalizeNullable(input.Avatar),
            Email = NormalizeNullable(input.Email),
            Phone = NormalizeNullable(input.Phone),
            Gender = input.Gender,
            Birthday = input.Birthday,
            Status = input.Status,
            TimeZone = NormalizeNullable(input.TimeZone),
            Language = NormalizeNullable(input.Language) ?? "zh-CN",
            Country = NormalizeNullable(input.Country),
            IsSystemAccount = false,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedUser = await _userRepository.AddAsync(user, cancellationToken);
        await CreateUserSecurityAsync(savedUser.BasicId, input.InitialPassword, now, input.Remark, cancellationToken);
        await CreateTenantMembershipAsync(savedUser.BasicId, input, now, cancellationToken);

        return UserApplicationMapper.ToDetailDto(savedUser);
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Update)]
    public async Task<UserDetailDto> UpdateUserAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var user = await GetUserOrThrowAsync(input.BasicId, cancellationToken);
        var normalizedEmail = NormalizeNullable(input.Email);
        var normalizedPhone = NormalizeNullable(input.Phone);
        var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var phoneChanged = !string.Equals(user.Phone, normalizedPhone, StringComparison.Ordinal);

        user.RealName = NormalizeNullable(input.RealName);
        user.NickName = NormalizeNullable(input.NickName);
        user.Avatar = NormalizeNullable(input.Avatar);
        user.Email = normalizedEmail;
        user.Phone = normalizedPhone;
        user.Gender = input.Gender;
        user.Birthday = input.Birthday;
        user.TimeZone = NormalizeNullable(input.TimeZone);
        user.Language = NormalizeNullable(input.Language);
        user.Country = NormalizeNullable(input.Country);
        user.Remark = NormalizeNullable(input.Remark);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        if (emailChanged || phoneChanged)
        {
            await ResetContactVerificationAsync(savedUser.BasicId, emailChanged, phoneChanged, cancellationToken);
        }

        return UserApplicationMapper.ToDetailDto(savedUser);
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Status)]
    public async Task<UserDetailDto> UpdateUserStatusAsync(UserStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));
        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");

        var user = await GetUserOrThrowAsync(input.BasicId, cancellationToken);
        if (input.Status == EnableStatus.Disabled)
        {
            await EnsureUserCanBeDisabledAsync(user, cancellationToken);
        }

        user.Status = input.Status;
        user.Remark = NormalizeNullable(input.Remark);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        if (input.Status == EnableStatus.Disabled)
        {
            await RefreshSecurityStampAsync(savedUser.BasicId, cancellationToken);
        }

        return UserApplicationMapper.ToDetailDto(savedUser);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Delete)]
    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await GetUserOrThrowAsync(id, cancellationToken);
        var membership = await EnsureUserCanBeDeletedAsync(user, cancellationToken);

        if (membership is not null)
        {
            membership.InviteStatus = TenantMemberInviteStatus.Revoked;
            membership.Status = ValidityStatus.Invalid;
            membership.RespondedTime ??= DateTimeOffset.UtcNow;
            _ = await _tenantUserRepository.UpdateAsync(membership, cancellationToken);
        }

        await SoftDeleteUserSecurityAsync(user.BasicId, cancellationToken);
        await _userRepository.SoftDeleteAsync(user, cancellationToken);
    }

    #endregion

    // ================================================================
    // #region 用户安全 (合并自 UserSecurityAppService)
    // ================================================================

    #region 用户安全

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="input">密码重置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.ResetPassword)]
    public async Task<UserSecurityDetailDto> ResetUserPasswordAsync(UserPasswordResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePasswordResetInput(input);

        var (user, security) = await GetUserSecurityOrThrowAsync(input.UserId, cancellationToken);
        await EnsurePasswordMeetsPolicyAsync(user, input.NewPassword, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        security.Password = _passwordHasher.HashPassword(input.NewPassword);
        security.LastPasswordChangeTime = now;
        security.PasswordExpiryTime = input.PasswordExpiryTime;
        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(input.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(savedSecurity, user, now);
    }

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    /// <param name="input">锁定状态参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.Lock)]
    public async Task<UserSecurityDetailDto> UpdateUserLockAsync(UserLockUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateLockInput(input);

        var (user, security) = await GetUserSecurityOrThrowAsync(input.UserId, cancellationToken);
        if (input.IsLocked)
        {
            await EnsureUserCanBeLockedAsync(user, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        if (input.IsLocked)
        {
            security.IsLocked = true;
            security.LockoutTime = now;
            security.LockoutEndTime = input.LockoutEndTime;
        }
        else
        {
            security.IsLocked = false;
            security.LockoutTime = null;
            security.LockoutEndTime = null;
            security.FailedLoginAttempts = 0;
            security.LastFailedLoginTime = null;
        }

        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(input.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(savedSecurity, user, now);
    }

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    /// <param name="input">登录策略参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.LoginPolicy)]
    public async Task<UserSecurityDetailDto> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateLoginPolicyInput(input);

        var (user, security) = await GetUserSecurityOrThrowAsync(input.UserId, cancellationToken);
        security.AllowMultiLogin = input.AllowMultiLogin;
        security.MaxLoginDevices = input.MaxLoginDevices;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(input.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(savedSecurity, user, DateTimeOffset.UtcNow);
    }

    #endregion

    // ================================================================
    // #region 用户角色 (合并自 UserRoleAppService)
    // ================================================================

    #region 用户角色

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

        ValidateUserRoleGrantInput(input);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, "分配角色", "平台管理员成员角色必须通过平台运维流程维护。", cancellationToken);
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

        ValidateUserRoleUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var userRole = await GetUserRoleOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, "分配角色", "平台管理员成员角色必须通过平台运维流程维护。", cancellationToken);
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
            ? await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, "分配角色", "平台管理员成员角色必须通过平台运维流程维护。", cancellationToken)
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

    #endregion

    // ================================================================
    // #region 用户直授权限 (合并自 UserPermissionAppService)
    // ================================================================

    #region 用户直授权限

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

        ValidateUserPermissionGrantInput(input);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, "维护直授权限", "平台管理员成员权限必须通过平台运维流程维护。", cancellationToken);
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

        ValidateUserPermissionUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var userPermission = await GetUserPermissionOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, "维护直授权限", "平台管理员成员权限必须通过平台运维流程维护。", cancellationToken);
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
            ? await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, "维护直授权限", "平台管理员成员权限必须通过平台运维流程维护。", cancellationToken)
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

    #endregion

    // ================================================================
    // #region 用户数据范围 (合并自 UserDataScopeAppService)
    // ================================================================

    #region 用户数据范围

    /// <summary>
    /// 授予用户数据范围
    /// </summary>
    /// <param name="input">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Grant)]
    public async Task<UserDataScopeDetailDto> CreateUserDataScopeAsync(UserDataScopeGrantDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDataScopeGrantInput(input);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, "维护数据范围", "平台管理员成员数据范围必须通过平台运维流程维护。", cancellationToken);
        var department = await GetDepartmentForScopeOrThrowAsync(input.DataScope, input.DepartmentId, cancellationToken);
        var departmentId = ResolveDataScopeDepartmentId(input.DataScope, input.DepartmentId);

        await EnsureCanPersistScopeAsync(input.UserId, input.DataScope, departmentId, null, cancellationToken);

        var dataScope = new SysUserDataScope
        {
            UserId = input.UserId,
            DataScope = input.DataScope,
            DepartmentId = departmentId,
            IncludeChildren = input.DataScope == DataPermissionScope.Custom && input.IncludeChildren,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedDataScope = await _userDataScopeRepository.AddAsync(dataScope, cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Update)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeAsync(UserDataScopeUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDataScopeUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var dataScope = await GetUserDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(dataScope.UserId, now, "维护数据范围", "平台管理员成员数据范围必须通过平台运维流程维护。", cancellationToken);
        var department = await GetDepartmentForScopeOrThrowAsync(input.DataScope, input.DepartmentId, cancellationToken);
        var departmentId = ResolveDataScopeDepartmentId(input.DataScope, input.DepartmentId);

        await EnsureCanPersistScopeAsync(dataScope.UserId, input.DataScope, departmentId, dataScope.BasicId, cancellationToken);

        dataScope.DataScope = input.DataScope;
        dataScope.DepartmentId = departmentId;
        dataScope.IncludeChildren = input.DataScope == DataPermissionScope.Custom && input.IncludeChildren;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Status)]
    public async Task<UserDataScopeDetailDto> UpdateUserDataScopeStatusAsync(UserDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var now = DateTimeOffset.UtcNow;
        var dataScope = await GetUserDataScopeOrThrowAsync(input.BasicId, cancellationToken);
        var tenantMember = input.Status == ValidityStatus.Valid
            ? await GetAssignableTenantMemberOrThrowAsync(dataScope.UserId, now, "维护数据范围", "平台管理员成员数据范围必须通过平台运维流程维护。", cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(dataScope.UserId, cancellationToken);
        var department = dataScope.DataScope == DataPermissionScope.Custom && input.Status == ValidityStatus.Valid
            ? await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken)
            : await GetDepartmentOrDefaultAsync(dataScope.DepartmentId, cancellationToken);

        dataScope.Status = input.Status;
        dataScope.Remark = NormalizeNullable(input.Remark);

        var savedDataScope = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return UserDataScopeApplicationMapper.ToDetailDto(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 撤销用户数据范围
    /// </summary>
    /// <param name="id">用户数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDataScope.Revoke)]
    public async Task DeleteUserDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await GetUserDataScopeOrThrowAsync(id, cancellationToken);
        dataScope.Status = ValidityStatus.Invalid;

        _ = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
    }

    #endregion

    // ================================================================
    // #region 用户部门 (合并自 UserDepartmentAppService)
    // ================================================================

    #region 用户部门

    /// <summary>
    /// 分配用户部门归属
    /// </summary>
    /// <param name="input">分配参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Grant)]
    public async Task<UserDepartmentDetailDto> CreateUserDepartmentAsync(UserDepartmentAssignDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDepartmentAssignInput(input);

        var now = DateTimeOffset.UtcNow;
        _ = await GetAssignableTenantMemberOrThrowAsync(input.UserId, now, "分配部门", "平台管理员成员部门归属必须通过平台运维流程维护。", cancellationToken);
        var department = await GetAssignableDepartmentOrThrowAsync(input.DepartmentId, cancellationToken);
        var shouldBeMain = input.IsMain || !await HasValidDepartmentAsync(input.UserId, cancellationToken);

        var userDepartment = await _userDepartmentRepository.GetFirstAsync(
            relation => relation.UserId == input.UserId && relation.DepartmentId == input.DepartmentId,
            cancellationToken);
        if (userDepartment is not null && userDepartment.Status == ValidityStatus.Valid)
        {
            throw new InvalidOperationException("用户部门归属已存在。");
        }

        if (shouldBeMain)
        {
            await ClearOtherMainDepartmentsAsync(input.UserId, userDepartment?.BasicId, cancellationToken);
        }

        if (userDepartment is null)
        {
            userDepartment = new SysUserDepartment
            {
                UserId = input.UserId,
                DepartmentId = input.DepartmentId,
                IsMain = shouldBeMain,
                Status = ValidityStatus.Valid,
                Remark = NormalizeNullable(input.Remark)
            };

            var savedUserDepartment = await _userDepartmentRepository.AddAsync(userDepartment, cancellationToken);
            return UserDepartmentApplicationMapper.ToDetailDto(savedUserDepartment, department);
        }

        userDepartment.IsMain = shouldBeMain;
        userDepartment.Status = ValidityStatus.Valid;
        userDepartment.Remark = NormalizeNullable(input.Remark);

        var restoredUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(restoredUserDepartment, department);
    }

    /// <summary>
    /// 更新用户部门归属
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Update)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentAsync(UserDepartmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDepartmentUpdateInput(input);

        var now = DateTimeOffset.UtcNow;
        var userDepartment = await GetUserDepartmentOrThrowAsync(input.BasicId, cancellationToken);
        if (userDepartment.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效用户部门归属不能更新。");
        }

        _ = await GetAssignableTenantMemberOrThrowAsync(userDepartment.UserId, now, "分配部门", "平台管理员成员部门归属必须通过平台运维流程维护。", cancellationToken);
        var department = await GetAssignableDepartmentOrThrowAsync(userDepartment.DepartmentId, cancellationToken);
        if (input.IsMain)
        {
            await ClearOtherMainDepartmentsAsync(userDepartment.UserId, userDepartment.BasicId, cancellationToken);
        }

        userDepartment.IsMain = input.IsMain;
        userDepartment.Remark = NormalizeNullable(input.Remark);

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        return UserDepartmentApplicationMapper.ToDetailDto(savedUserDepartment, department);
    }

    /// <summary>
    /// 更新用户部门归属状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Status)]
    public async Task<UserDepartmentDetailDto> UpdateUserDepartmentStatusAsync(UserDepartmentStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户部门归属主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var now = DateTimeOffset.UtcNow;
        var userDepartment = await GetUserDepartmentOrThrowAsync(input.BasicId, cancellationToken);
        var department = input.Status == ValidityStatus.Valid
            ? await GetAssignableDepartmentOrThrowAsync(userDepartment.DepartmentId, cancellationToken)
            : await _departmentRepository.GetByIdAsync(userDepartment.DepartmentId, cancellationToken);

        if (input.Status == ValidityStatus.Valid)
        {
            _ = await GetAssignableTenantMemberOrThrowAsync(userDepartment.UserId, now, "分配部门", "平台管理员成员部门归属必须通过平台运维流程维护。", cancellationToken);
            if (userDepartment.IsMain)
            {
                await ClearOtherMainDepartmentsAsync(userDepartment.UserId, userDepartment.BasicId, cancellationToken);
            }
        }
        else
        {
            userDepartment.IsMain = false;
        }

        userDepartment.Status = input.Status;
        userDepartment.Remark = NormalizeNullable(input.Remark);

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        if (input.Status != ValidityStatus.Valid)
        {
            await PromoteMainDepartmentIfNeededAsync(savedUserDepartment.UserId, savedUserDepartment.BasicId, cancellationToken);
        }

        return UserDepartmentApplicationMapper.ToDetailDto(savedUserDepartment, department);
    }

    /// <summary>
    /// 撤销用户部门归属
    /// </summary>
    /// <param name="id">用户部门归属主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserDepartment.Revoke)]
    public async Task DeleteUserDepartmentAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userDepartment = await GetUserDepartmentOrThrowAsync(id, cancellationToken);
        userDepartment.IsMain = false;
        userDepartment.Status = ValidityStatus.Invalid;

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        await PromoteMainDepartmentIfNeededAsync(savedUserDepartment.UserId, savedUserDepartment.BasicId, cancellationToken);
    }

    #endregion

    // ================================================================
    // #region 用户会话 (合并自 UserSessionAppService)
    // ================================================================

    #region 用户会话

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    /// <param name="input">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<UserSessionDetailDto> RevokeUserSessionAsync(UserSessionRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRevokeInput(input.BasicId, input.Reason, "会话主键必须大于 0。");

        var session = await GetSessionOrThrowAsync(input.BasicId, cancellationToken);
        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        if (!session.IsRevoked)
        {
            var reason = input.Reason.Trim();
            RevokeSession(session, reason, DateTimeOffset.UtcNow);
            session = await _userSessionRepository.UpdateAsync(session, cancellationToken);
            await PublishSessionRevokedAsync(session, revokeAllUserSessions: false, reason, cancellationToken);
        }

        return UserSessionApplicationMapper.ToDetailDto(session, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 撤销用户全部会话
    /// </summary>
    /// <param name="input">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销会话数量</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<int> RevokeUserSessionsAsync(UserSessionsRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRevokeInput(input.UserId, input.Reason, "用户主键必须大于 0。");

        var user = await _userRepository.GetByIdAsync(input.UserId, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
        var sessions = await _userSessionRepository.GetListAsync(
            session => session.UserId == user.BasicId && !session.IsRevoked,
            cancellationToken);

        if (sessions.Count == 0)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        var reason = input.Reason.Trim();
        foreach (var session in sessions)
        {
            RevokeSession(session, reason, now);
        }

        _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        await PublishUserSessionsRevokedAsync(user, sessions[0].TenantId, reason, cancellationToken);
        return sessions.Count;
    }

    #endregion

    // ================================================================
    // #region 私有辅助方法 (统一管理)
    // ================================================================

    #region 私有辅助方法

    // ---- 用户核心辅助 ----

    /// <summary>
    /// 创建用户安全记录
    /// </summary>
    private async Task CreateUserSecurityAsync(long userId, string password, DateTimeOffset now, string? remark, CancellationToken cancellationToken)
    {
        var userSecurity = new SysUserSecurity
        {
            UserId = userId,
            Password = _passwordHasher.HashPassword(password),
            LastPasswordChangeTime = now,
            FailedLoginAttempts = 0,
            IsLocked = false,
            TwoFactorEnabled = false,
            TwoFactorMethod = TwoFactorMethod.None,
            SecurityStamp = NewSecurityStamp(),
            EmailVerified = false,
            PhoneVerified = false,
            AllowMultiLogin = true,
            MaxLoginDevices = 0,
            LastSecurityCheckTime = now,
            Remark = NormalizeNullable(remark)
        };

        _ = await _userSecurityRepository.AddAsync(userSecurity, cancellationToken);
    }

    /// <summary>
    /// 创建当前租户成员记录
    /// </summary>
    private async Task CreateTenantMembershipAsync(long userId, UserCreateDto input, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = new SysTenantUser
        {
            UserId = userId,
            MemberType = input.MemberType,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            InvitedBy = _currentUser.UserId,
            InvitedTime = now,
            RespondedTime = now,
            EffectiveTime = input.EffectiveTime,
            ExpirationTime = input.ExpirationTime,
            DisplayName = NormalizeNullable(input.DisplayName),
            InviteRemark = NormalizeNullable(input.InviteRemark),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(input.Remark)
        };

        _ = await _tenantUserRepository.AddAsync(tenantMember, cancellationToken);
    }

    /// <summary>
    /// 获取用户，不存在时抛出异常
    /// </summary>
    private async Task<SysUser> GetUserOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户主键必须大于 0。");
        }

        return await _userRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
    }

    /// <summary>
    /// 校验用户能否停用
    /// </summary>
    private async Task EnsureUserCanBeDisabledAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能停用。");
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能直接停用。");
        }
    }

    /// <summary>
    /// 校验用户能否删除
    /// </summary>
    private async Task<SysTenantUser?> EnsureUserCanBeDeletedAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能删除。");
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能直接删除。");
        }

        return membership;
    }

    /// <summary>
    /// 重置联系方式验证状态
    /// </summary>
    private async Task ResetContactVerificationAsync(long userId, bool emailChanged, bool phoneChanged, CancellationToken cancellationToken)
    {
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == userId, cancellationToken);
        if (security is null)
        {
            return;
        }

        if (emailChanged)
        {
            security.EmailVerified = false;
        }

        if (phoneChanged)
        {
            security.PhoneVerified = false;
        }

        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <summary>
    /// 刷新安全戳
    /// </summary>
    private async Task RefreshSecurityStampAsync(long userId, CancellationToken cancellationToken)
    {
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == userId, cancellationToken);
        if (security is null)
        {
            return;
        }

        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    /// <summary>
    /// 软删除用户安全记录
    /// </summary>
    private async Task SoftDeleteUserSecurityAsync(long userId, CancellationToken cancellationToken)
    {
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == userId, cancellationToken);
        if (security is null)
        {
            return;
        }

        security.IsDeleted = true;
        security.DeletedTime = DateTimeOffset.UtcNow;
        security.SecurityStamp = NewSecurityStamp();
        _ = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
    }

    // ---- 用户安全辅助 ----

    /// <summary>
    /// 获取用户与安全记录，不存在时抛出异常
    /// </summary>
    private async Task<(SysUser User, SysUserSecurity Security)> GetUserSecurityOrThrowAsync(long userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "用户主键必须大于 0。");
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
        var security = await _userSecurityRepository.GetFirstAsync(item => item.UserId == user.BasicId, cancellationToken)
            ?? throw new InvalidOperationException("用户安全记录不存在。");

        return (user, security);
    }

    /// <summary>
    /// 校验用户能否被锁定
    /// </summary>
    private async Task EnsureUserCanBeLockedAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能通过用户安全服务锁定。");
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能通过用户安全服务锁定。");
        }
    }

    /// <summary>
    /// 校验密码策略 (SysUser 重载)
    /// </summary>
    private async Task EnsurePasswordMeetsPolicyAsync(SysUser user, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var blacklist = BuildPasswordBlacklist(user);
        var result = await _authenticationService.ValidatePasswordStrengthAsync(password, blacklist);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors.Count > 0 ? string.Join("；", result.Errors) : result.Message;
        throw new InvalidOperationException($"新密码不符合安全要求：{errors}");
    }

    /// <summary>
    /// 构建密码黑名单 (SysUser 重载)
    /// </summary>
    private static List<string> BuildPasswordBlacklist(SysUser user)
    {
        return
        [
            .. new[]
            {
                user.UserName,
                user.RealName,
                user.NickName,
                user.Email,
                user.Phone
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
        ];
    }

    // ---- 用户角色辅助 ----

    /// <summary>
    /// 获取用户角色绑定，不存在时抛出异常
    /// </summary>
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
    /// 获取可授权角色，不满足规则时抛出异常
    /// </summary>
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

    // ---- 用户直授权限辅助 ----

    /// <summary>
    /// 获取用户直授权限绑定，不存在时抛出异常
    /// </summary>
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
    /// 获取可授权权限，不满足规则时抛出异常
    /// </summary>
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

    // ---- 用户数据范围辅助 ----

    /// <summary>
    /// 获取用户数据范围绑定，不存在时抛出异常
    /// </summary>
    private async Task<SysUserDataScope> GetUserDataScopeOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户数据范围绑定主键必须大于 0。");
        }

        return await _userDataScopeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户数据范围绑定不存在。");
    }

    /// <summary>
    /// 校验用户数据范围覆盖可持久化
    /// </summary>
    private async Task EnsureCanPersistScopeAsync(long userId, DataPermissionScope dataScope, long departmentId, long? excludeId, CancellationToken cancellationToken)
    {
        var hasOtherMode = dataScope == DataPermissionScope.Custom
            ? await _userDataScopeRepository.AnyAsync(
                scope => scope.UserId == userId && scope.DataScope != DataPermissionScope.Custom && (!excludeId.HasValue || scope.BasicId != excludeId.Value),
                cancellationToken)
            : await _userDataScopeRepository.AnyAsync(
                scope => scope.UserId == userId && (!excludeId.HasValue || scope.BasicId != excludeId.Value),
                cancellationToken);

        if (hasOtherMode)
        {
            throw new InvalidOperationException("用户数据范围覆盖模式冲突。");
        }

        if (await _userDataScopeRepository.AnyAsync(
            scope => scope.UserId == userId && scope.DepartmentId == departmentId && (!excludeId.HasValue || scope.BasicId != excludeId.Value),
            cancellationToken))
        {
            throw new InvalidOperationException("用户数据范围已绑定。");
        }
    }

    /// <summary>
    /// 获取数据范围对应部门，不满足规则时抛出异常
    /// </summary>
    private async Task<SysDepartment?> GetDepartmentForScopeOrThrowAsync(DataPermissionScope dataScope, long? departmentId, CancellationToken cancellationToken)
    {
        if (dataScope == DataPermissionScope.Custom)
        {
            if (!departmentId.HasValue || departmentId.Value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(departmentId), "自定义用户数据范围必须指定部门主键。");
            }

            return await GetEnabledDepartmentOrThrowAsync(departmentId.Value, cancellationToken);
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            throw new InvalidOperationException("非自定义用户数据范围不能指定部门。");
        }

        return null;
    }

    /// <summary>
    /// 获取已启用部门，不满足规则时抛出异常
    /// </summary>
    private async Task<SysDepartment> GetEnabledDepartmentOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能绑定到用户数据范围。");
        }

        return department;
    }

    /// <summary>
    /// 按需获取部门
    /// </summary>
    private async Task<SysDepartment?> GetDepartmentOrDefaultAsync(long departmentId, CancellationToken cancellationToken)
    {
        return departmentId > 0
            ? await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            : null;
    }

    /// <summary>
    /// 解析持久化部门主键
    /// </summary>
    private static long ResolveDataScopeDepartmentId(DataPermissionScope dataScope, long? departmentId)
    {
        return dataScope == DataPermissionScope.Custom ? departmentId!.Value : 0;
    }

    // ---- 用户部门辅助 ----

    /// <summary>
    /// 获取用户部门归属，不存在时抛出异常
    /// </summary>
    private async Task<SysUserDepartment> GetUserDepartmentOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户部门归属主键必须大于 0。");
        }

        return await _userDepartmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户部门归属不存在。");
    }

    /// <summary>
    /// 获取可分配部门，不满足规则时抛出异常
    /// </summary>
    private async Task<SysDepartment> GetAssignableDepartmentOrThrowAsync(long departmentId, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");

        if (department.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用部门不能分配给用户。");
        }

        return department;
    }

    /// <summary>
    /// 判断用户是否已有有效部门归属
    /// </summary>
    private async Task<bool> HasValidDepartmentAsync(long userId, CancellationToken cancellationToken)
    {
        return await _userDepartmentRepository.AnyAsync(
            relation => relation.UserId == userId && relation.Status == ValidityStatus.Valid,
            cancellationToken);
    }

    /// <summary>
    /// 清理用户其它主部门标记
    /// </summary>
    private async Task ClearOtherMainDepartmentsAsync(long userId, long? excludeId, CancellationToken cancellationToken)
    {
        var mainDepartments = await _userDepartmentRepository.GetListAsync(
            relation => relation.UserId == userId && relation.IsMain && (!excludeId.HasValue || relation.BasicId != excludeId.Value),
            cancellationToken);
        if (mainDepartments.Count == 0)
        {
            return;
        }

        foreach (var mainDepartment in mainDepartments)
        {
            mainDepartment.IsMain = false;
        }

        await _userDepartmentRepository.UpdateRangeAsync(mainDepartments, cancellationToken);
    }

    /// <summary>
    /// 用户撤销主部门后自动接续一个有效部门
    /// </summary>
    private async Task PromoteMainDepartmentIfNeededAsync(long userId, long revokedId, CancellationToken cancellationToken)
    {
        if (await _userDepartmentRepository.AnyAsync(
            relation => relation.UserId == userId && relation.Status == ValidityStatus.Valid && relation.IsMain,
            cancellationToken))
        {
            return;
        }

        var candidates = await _userDepartmentRepository.GetListAsync(
            relation => relation.UserId == userId && relation.Status == ValidityStatus.Valid && relation.BasicId != revokedId,
            relation => relation.CreatedTime,
            cancellationToken);
        var nextMain = candidates.FirstOrDefault();
        if (nextMain is null)
        {
            return;
        }

        nextMain.IsMain = true;
        _ = await _userDepartmentRepository.UpdateAsync(nextMain, cancellationToken);
    }

    // ---- 用户会话辅助 ----

    /// <summary>
    /// 获取会话，不存在时抛出异常
    /// </summary>
    private async Task<SysUserSession> GetSessionOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        return await _userSessionRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("用户会话不存在。");
    }

    /// <summary>
    /// 撤销会话
    /// </summary>
    private static void RevokeSession(SysUserSession session, string reason, DateTimeOffset now)
    {
        session.IsRevoked = true;
        session.RevokedAt = now;
        session.RevokedReason = reason;
        session.IsOnline = false;
        session.LogoutTime ??= now;
        session.LastActivityTime = now;
    }

    /// <summary>
    /// 发布单会话撤销事件
    /// </summary>
    private async Task PublishSessionRevokedAsync(SysUserSession session, bool revokeAllUserSessions, string reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _localEventBus.PublishAsync(
            new UserSessionRevokedDomainEvent(
                session.TenantId,
                session.UserId,
                session.BasicId,
                session.UserSessionId,
                session.CurrentAccessTokenJti,
                revokeAllUserSessions,
                _currentUser.UserId,
                reason));
    }

    /// <summary>
    /// 发布用户全部会话撤销事件
    /// </summary>
    private async Task PublishUserSessionsRevokedAsync(SysUser user, long sessionTenantKey, string reason, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _localEventBus.PublishAsync(
            new UserSessionRevokedDomainEvent(
                sessionTenantKey,
                user.BasicId,
                sessionId: null,
                userSessionId: null,
                accessTokenJti: null,
                revokeAllUserSessions: true,
                _currentUser.UserId,
                reason));
    }

    // ---- 共享辅助 (可授权租户成员) ----

    /// <summary>
    /// 获取可操作租户成员，不满足规则时抛出异常
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="now">当前时间</param>
    /// <param name="operationContext">操作上下文描述（如"分配角色"）</param>
    /// <param name="platformAdminMessage">平台管理员拦截消息</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task<SysTenantUser> GetAssignableTenantMemberOrThrowAsync(
        long userId, DateTimeOffset now, string operationContext, string platformAdminMessage, CancellationToken cancellationToken)
    {
        var tenantMember = await _tenantUserRepository.GetMembershipAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("当前租户成员不存在。");

        if (tenantMember.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException($"未接受邀请的租户成员不能{operationContext}。");
        }

        if (tenantMember.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException($"无效租户成员不能{operationContext}。");
        }

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException(platformAdminMessage);
        }

        if (tenantMember.EffectiveTime.HasValue && tenantMember.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException($"未生效租户成员不能{operationContext}。");
        }

        if (tenantMember.ExpirationTime.HasValue && tenantMember.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException($"已过期租户成员不能{operationContext}。");
        }

        return tenantMember;
    }

    // ---- 共享校验 ----

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(UserCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.UserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(input.InitialPassword);

        ValidateRequiredLength(input.UserName, 50, nameof(input.UserName), "用户名不能超过 50 个字符。");
        ValidateOptionalLength(input.RealName, 50, nameof(input.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(input.NickName, 50, nameof(input.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(input.Avatar, 500, nameof(input.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(input.Email, 100, nameof(input.Email), "邮箱不能超过 100 个字符。");
        ValidateOptionalLength(input.Phone, 20, nameof(input.Phone), "手机号不能超过 20 个字符。");
        ValidateOptionalLength(input.TimeZone, 50, nameof(input.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(input.Language, 10, nameof(input.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(input.Country, 50, nameof(input.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(input.DisplayName, 100, nameof(input.DisplayName), "租户内显示名不能超过 100 个字符。");
        ValidateOptionalLength(input.InviteRemark, 500, nameof(input.InviteRemark), "邀请备注不能超过 500 个字符。");
        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        ValidateEnum(input.Gender, nameof(input.Gender));
        ValidateEnum(input.Status, nameof(input.Status));
        ValidateEnum(input.MemberType, nameof(input.MemberType));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime, "成员");
        EnsureMemberTypeCanBeCreated(input.MemberType);
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(UserUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ValidateOptionalLength(input.RealName, 50, nameof(input.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(input.NickName, 50, nameof(input.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(input.Avatar, 500, nameof(input.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(input.Email, 100, nameof(input.Email), "邮箱不能超过 100 个字符。");
        ValidateOptionalLength(input.Phone, 20, nameof(input.Phone), "手机号不能超过 20 个字符。");
        ValidateOptionalLength(input.TimeZone, 50, nameof(input.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(input.Language, 10, nameof(input.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(input.Country, 50, nameof(input.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
        ValidateEnum(input.Gender, nameof(input.Gender));
    }

    /// <summary>
    /// 校验密码策略 (UserCreateDto 重载)
    /// </summary>
    private async Task EnsurePasswordMeetsPolicyAsync(UserCreateDto input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var blacklist = BuildPasswordBlacklist(input);
        var result = await _authenticationService.ValidatePasswordStrengthAsync(input.InitialPassword, blacklist);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors.Count > 0 ? string.Join("；", result.Errors) : result.Message;
        throw new InvalidOperationException($"初始密码不符合安全要求：{errors}");
    }

    /// <summary>
    /// 构建密码黑名单 (UserCreateDto 重载)
    /// </summary>
    private static List<string> BuildPasswordBlacklist(UserCreateDto input)
    {
        return
        [
            .. new[]
            {
                input.UserName,
                input.RealName,
                input.NickName,
                input.Email,
                input.Phone
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
        ];
    }

    /// <summary>
    /// 校验成员类型可由用户创建流程分配
    /// </summary>
    private static void EnsureMemberTypeCanBeCreated(TenantMemberType memberType)
    {
        if (memberType is TenantMemberType.Owner or TenantMemberType.PlatformAdmin)
        {
            throw new InvalidOperationException("租户所有者和平台管理员成员身份必须通过专项流程维护。");
        }
    }

    /// <summary>
    /// 校验密码重置参数
    /// </summary>
    private static void ValidatePasswordResetInput(UserPasswordResetDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(input.NewPassword);
        if (input.PasswordExpiryTime.HasValue && input.PasswordExpiryTime.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "密码过期时间必须晚于当前时间。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验锁定参数
    /// </summary>
    private static void ValidateLockInput(UserLockUpdateDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.IsLocked && input.LockoutEndTime.HasValue && input.LockoutEndTime.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "锁定结束时间必须晚于当前时间。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验登录策略参数
    /// </summary>
    private static void ValidateLoginPolicyInput(UserLoginPolicyUpdateDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.MaxLoginDevices < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "最大登录设备数不能小于 0。");
        }

        if (!input.AllowMultiLogin && input.MaxLoginDevices == 0)
        {
            throw new InvalidOperationException("禁用多端登录时最大登录设备数必须大于 0。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验用户角色授权参数
    /// </summary>
    private static void ValidateUserRoleGrantInput(UserRoleGrantDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "角色主键必须大于 0。");
        }

        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime, "用户角色");
    }

    /// <summary>
    /// 校验用户角色更新参数
    /// </summary>
    private static void ValidateUserRoleUpdateInput(UserRoleUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户角色绑定主键必须大于 0。");
        }

        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime, "用户角色");
    }

    /// <summary>
    /// 校验用户直授权限授权参数
    /// </summary>
    private static void ValidateUserPermissionGrantInput(UserPermissionGrantDto input)
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
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime, "用户直授权限");
    }

    /// <summary>
    /// 校验用户直授权限更新参数
    /// </summary>
    private static void ValidateUserPermissionUpdateInput(UserPermissionUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户直授权限绑定主键必须大于 0。");
        }

        ValidateEnum(input.PermissionAction, nameof(input.PermissionAction));
        ValidateEffectivePeriod(input.EffectiveTime, input.ExpirationTime, "用户直授权限");
    }

    /// <summary>
    /// 校验用户数据范围授权参数
    /// </summary>
    private static void ValidateDataScopeGrantInput(UserDataScopeGrantDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        ValidateEnum(input.DataScope, nameof(input.DataScope));
    }

    /// <summary>
    /// 校验用户数据范围更新参数
    /// </summary>
    private static void ValidateDataScopeUpdateInput(UserDataScopeUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(input.DataScope, nameof(input.DataScope));
    }

    /// <summary>
    /// 校验用户部门分配参数
    /// </summary>
    private static void ValidateDepartmentAssignInput(UserDepartmentAssignDto input)
    {
        if (input.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户主键必须大于 0。");
        }

        if (input.DepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "部门主键必须大于 0。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验用户部门更新参数
    /// </summary>
    private static void ValidateDepartmentUpdateInput(UserDepartmentUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "用户部门归属主键必须大于 0。");
        }

        ValidateOptionalLength(input.Remark, 500, nameof(input.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验撤销参数
    /// </summary>
    private static void ValidateRevokeInput(long id, string reason, string idMessage)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), idMessage);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (reason.Trim().Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(reason), "撤销原因不能超过 200 个字符。");
        }
    }

    /// <summary>
    /// 校验有效期
    /// </summary>
    private static void ValidateEffectivePeriod(DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime, string domainLabel)
    {
        if (effectiveTime.HasValue && expirationTime.HasValue && expirationTime.Value <= effectiveTime.Value)
        {
            throw new InvalidOperationException($"{domainLabel}失效时间必须晚于生效时间。");
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验必填字符串长度
    /// </summary>
    private static void ValidateRequiredLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// 创建安全戳
    /// </summary>
    private static string NewSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }

    #endregion
}
