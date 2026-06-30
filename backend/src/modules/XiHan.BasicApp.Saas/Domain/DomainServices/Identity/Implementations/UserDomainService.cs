#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserDomainService
// Guid:5168ff6e-6f63-42e6-a251-f789e69fe8f7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Password;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 用户领域服务
/// 负责用户资料、安全、角色、权限、数据范围、部门归属与会话撤销等命令编排
/// </summary>
public sealed class UserDomainService
    : IUserDomainService
{
    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository;

    // ================================================================
    // 字段 — 用户核心
    // ================================================================
    /// <summary>
    /// 用户安全仓储
    /// </summary>
    private readonly IUserSecurityRepository _userSecurityRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository;

    /// <summary>
    /// 密码哈希服务
    /// </summary>
    private readonly IPasswordHasher _passwordHasher;

    /// <summary>
    /// 认证服务
    /// </summary>
    private readonly IAuthenticationService _authenticationService;

    /// <summary>
    /// 用户角色仓储
    /// </summary>
    private readonly IUserRoleRepository _userRoleRepository;

    // ================================================================
    // 字段 — 用户角色
    // ================================================================
    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 用户直授权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository;

    // ================================================================
    // 字段 — 用户直授权限
    // ================================================================
    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 用户数据范围仓储
    /// </summary>
    private readonly IUserDataScopeRepository _userDataScopeRepository;

    // ================================================================
    // 字段 — 用户数据范围
    // ================================================================
    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 用户部门仓储
    /// </summary>
    private readonly IUserDepartmentRepository _userDepartmentRepository;

    /// <summary>
    /// 用户会话仓储
    /// </summary>
    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 当前租户
    /// </summary>
    private readonly ICurrentTenant _currentTenant;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserDomainService(
        IUserRepository userRepository,
        IUserSecurityRepository userSecurityRepository,
        ITenantUserRepository tenantUserRepository,
        IPasswordHasher passwordHasher,
        IAuthenticationService authenticationService,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IUserPermissionRepository userPermissionRepository,
        IPermissionRepository permissionRepository,
        IUserDataScopeRepository userDataScopeRepository,
        IDepartmentRepository departmentRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IUserSessionRepository userSessionRepository,
        ICurrentTenant currentTenant)
    {
        _userRepository = userRepository;
        _userSecurityRepository = userSecurityRepository;
        _tenantUserRepository = tenantUserRepository;
        _passwordHasher = passwordHasher;
        _authenticationService = authenticationService;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _userPermissionRepository = userPermissionRepository;
        _permissionRepository = permissionRepository;
        _userDataScopeRepository = userDataScopeRepository;
        _departmentRepository = departmentRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _userSessionRepository = userSessionRepository;
        _currentTenant = currentTenant;
    }

    // ================================================================
    // 字段 — 用户部门归属
    // ================================================================
    // ================================================================
    // 字段 — 用户会话
    // ================================================================
    // ================================================================
    // #region 用户核心 (原始 UserAppService 方法)
    // ================================================================

    #region 用户核心

    /// <summary>
    /// 创建用户
    /// </summary>
    /// <param name="command">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    public async Task<UserCommandResult> CreateUserAsync(UserCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);

        var userName = command.UserName.Trim();
        if (await _userRepository.ExistsUserNameAsync(userName, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("用户名已存在。");
        }

        await EnsureEmailUniqueAsync(NormalizeNullable(command.Email), excludeUserId: null, cancellationToken);
        await EnsurePasswordMeetsPolicyAsync(command, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var user = new SysUser
        {
            UserName = userName,
            RealName = NormalizeNullable(command.RealName),
            NickName = NormalizeNullable(command.NickName),
            Avatar = NormalizeNullable(command.Avatar),
            Email = NormalizeNullable(command.Email),
            Phone = NormalizeNullable(command.Phone),
            Gender = command.Gender,
            Birthday = command.Birthday,
            Status = command.Status,
            TimeZone = NormalizeNullable(command.TimeZone),
            Language = NormalizeNullable(command.Language) ?? "zh-CN",
            Country = NormalizeNullable(command.Country),
            IsSystemAccount = false,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedUser = await _userRepository.AddAsync(user, cancellationToken);
        await CreateUserSecurityAsync(savedUser.BasicId, command.InitialPassword, now, command.Remark, cancellationToken);
        await CreateTenantMembershipAsync(savedUser.BasicId, command, now, cancellationToken);

        return new UserCommandResult(savedUser);
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    /// <param name="command">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    public async Task<UserCommandResult> UpdateUserAsync(UserUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);

        var user = await GetUserOrThrowAsync(command.BasicId, cancellationToken);
        var normalizedEmail = NormalizeNullable(command.Email);
        var normalizedPhone = NormalizeNullable(command.Phone);
        var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
        var phoneChanged = !string.Equals(user.Phone, normalizedPhone, StringComparison.Ordinal);

        if (emailChanged)
        {
            await EnsureEmailUniqueAsync(normalizedEmail, user.BasicId, cancellationToken);
        }

        user.RealName = NormalizeNullable(command.RealName);
        user.NickName = NormalizeNullable(command.NickName);
        user.Avatar = NormalizeNullable(command.Avatar);
        user.Email = normalizedEmail;
        user.Phone = normalizedPhone;
        user.Gender = command.Gender;
        user.Birthday = command.Birthday;
        user.TimeZone = NormalizeNullable(command.TimeZone);
        user.Language = NormalizeNullable(command.Language);
        user.Country = NormalizeNullable(command.Country);
        user.Remark = NormalizeNullable(command.Remark);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        if (emailChanged || phoneChanged)
        {
            await ResetContactVerificationAsync(savedUser.BasicId, emailChanged, phoneChanged, cancellationToken);
        }

        return new UserCommandResult(savedUser);
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    /// <param name="command">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详情</returns>
    public async Task<UserCommandResult> UpdateUserStatusAsync(UserStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));
        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");

        var user = await GetUserOrThrowAsync(command.BasicId, cancellationToken);
        if (command.Status == EnableStatus.Disabled)
        {
            await EnsureUserCanBeDisabledAsync(user, cancellationToken);
        }

        user.Status = command.Status;
        user.Remark = NormalizeNullable(command.Remark);

        var savedUser = await _userRepository.UpdateAsync(user, cancellationToken);
        if (command.Status == EnableStatus.Disabled)
        {
            await RefreshSecurityStampAsync(savedUser.BasicId, cancellationToken);
        }

        return new UserCommandResult(savedUser);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="id">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
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

    #region 用户安全

    /// <summary>
    /// 重置用户密码
    /// </summary>
    /// <param name="command">密码重置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    public async Task<UserSecurityCommandResult> ResetUserPasswordAsync(UserPasswordResetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePasswordResetCommand(command);

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        await EnsureUserCanBeResetAsync(user, cancellationToken);
        await EnsurePasswordMeetsPolicyAsync(user, command.NewPassword, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        security.Password = _passwordHasher.HashPassword(command.NewPassword);
        security.LastPasswordChangeTime = now;
        security.PasswordExpirationTime = command.PasswordExpirationTime;
        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(command.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new UserSecurityCommandResult(savedSecurity, user, now);
    }

    /// <summary>
    /// 重置用户双因素认证（清除 OTP 绑定）
    /// </summary>
    /// <remarks>
    /// 清除双因素开关、方式与密钥并刷新安全戳；用户下次登录不再要求 OTP，可在个人中心重新绑定。
    /// </remarks>
    /// <param name="command">双因素重置参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    public async Task<UserSecurityCommandResult> ResetUserTwoFactorAsync(UserTwoFactorResetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        security.TwoFactorEnabled = false;
        security.TwoFactorMethod = TwoFactorMethod.None;
        security.TwoFactorSecret = null;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(command.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new UserSecurityCommandResult(savedSecurity, user, now);
    }

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    /// <param name="command">锁定状态参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    public async Task<UserSecurityCommandResult> UpdateUserLockAsync(UserLockChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateLockCommand(command);

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        if (command.IsLocked)
        {
            await EnsureUserCanBeLockedAsync(user, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        if (command.IsLocked)
        {
            security.IsLocked = true;
            security.LockoutTime = now;
            security.LockoutEndTime = command.LockoutEndTime;
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
        security.Remark = NormalizeNullable(command.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new UserSecurityCommandResult(savedSecurity, user, now);
    }

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    /// <param name="command">登录策略参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户安全详情</returns>
    public async Task<UserSecurityCommandResult> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateLoginPolicyCommand(command);

        var (user, security) = await GetUserSecurityOrThrowAsync(command.UserId, cancellationToken);
        security.AllowMultiLogin = command.AllowMultiLogin;
        security.MaxLoginDevices = command.MaxLoginDevices;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(command.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        return new UserSecurityCommandResult(savedSecurity, user, DateTimeOffset.UtcNow);
    }

    #endregion

    #region 用户角色

    /// <summary>
    /// 授予用户角色
    /// </summary>
    /// <param name="command">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    public async Task<UserRoleCommandResult> CreateUserRoleAsync(UserRoleGrantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUserRoleGrantCommand(command);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(command.UserId, now, "分配角色", "平台管理员成员角色必须通过平台运维流程维护。", cancellationToken);
        var role = await GetAssignableRoleOrThrowAsync(command.RoleId, cancellationToken);
        if (await _userRoleRepository.AnyAsync(
            userRole => userRole.UserId == command.UserId && userRole.RoleId == command.RoleId,
            cancellationToken))
        {
            throw new InvalidOperationException("用户角色已绑定。");
        }

        var userRole = new SysUserRole
        {
            UserId = command.UserId,
            RoleId = command.RoleId,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            GrantReason = NormalizeNullable(command.GrantReason),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedUserRole = await _userRoleRepository.AddAsync(userRole, cancellationToken);
        return new UserRoleCommandResult(savedUserRole, role, tenantMember, now);
    }

    /// <summary>
    /// 更新用户角色
    /// </summary>
    /// <param name="command">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    public async Task<UserRoleCommandResult> UpdateUserRoleAsync(UserRoleUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUserRoleUpdateCommand(command);

        var now = DateTimeOffset.UtcNow;
        var userRole = await GetUserRoleOrThrowAsync(command.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, "分配角色", "平台管理员成员角色必须通过平台运维流程维护。", cancellationToken);
        var role = await GetAssignableRoleOrThrowAsync(userRole.RoleId, cancellationToken);

        userRole.EffectiveTime = command.EffectiveTime;
        userRole.ExpirationTime = command.ExpirationTime;
        userRole.GrantReason = NormalizeNullable(command.GrantReason);
        userRole.Remark = NormalizeNullable(command.Remark);

        var savedUserRole = await _userRoleRepository.UpdateAsync(userRole, cancellationToken);
        return new UserRoleCommandResult(savedUserRole, role, tenantMember, now);
    }

    /// <summary>
    /// 更新用户角色状态
    /// </summary>
    /// <param name="command">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户角色详情</returns>
    public async Task<UserRoleCommandResult> UpdateUserRoleStatusAsync(UserRoleStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户角色绑定主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var now = DateTimeOffset.UtcNow;
        var userRole = await GetUserRoleOrThrowAsync(command.BasicId, cancellationToken);
        var tenantMember = command.Status == ValidityStatus.Valid
            ? await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, "分配角色", "平台管理员成员角色必须通过平台运维流程维护。", cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(userRole.UserId, cancellationToken);
        var role = command.Status == ValidityStatus.Valid
            ? await GetAssignableRoleOrThrowAsync(userRole.RoleId, cancellationToken)
            : await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);

        userRole.Status = command.Status;
        userRole.Remark = NormalizeNullable(command.Remark);

        var savedUserRole = await _userRoleRepository.UpdateAsync(userRole, cancellationToken);
        return new UserRoleCommandResult(savedUserRole, role, tenantMember, now);
    }

    /// <summary>
    /// 撤销用户角色
    /// </summary>
    /// <param name="id">用户角色绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteUserRoleAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userRole = await GetUserRoleOrThrowAsync(id, cancellationToken);
        userRole.Status = ValidityStatus.Invalid;

        _ = await _userRoleRepository.UpdateAsync(userRole, cancellationToken);
    }

    #endregion

    #region 用户直授权限

    /// <summary>
    /// 授予用户直授权限
    /// </summary>
    /// <param name="command">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    public async Task<UserPermissionCommandResult> CreateUserPermissionAsync(UserPermissionGrantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUserPermissionGrantCommand(command);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(command.UserId, now, "维护直授权限", "平台管理员成员权限必须通过平台运维流程维护。", cancellationToken);
        var permission = await GetGrantablePermissionOrThrowAsync(command.PermissionId, cancellationToken);
        if (await _userPermissionRepository.AnyAsync(
            userPermission => userPermission.UserId == command.UserId && userPermission.PermissionId == command.PermissionId,
            cancellationToken))
        {
            throw new InvalidOperationException("用户直授权限已绑定。");
        }

        var userPermission = new SysUserPermission
        {
            UserId = command.UserId,
            PermissionId = command.PermissionId,
            PermissionAction = command.PermissionAction,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            GrantReason = NormalizeNullable(command.GrantReason),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedUserPermission = await _userPermissionRepository.AddAsync(userPermission, cancellationToken);
        return new UserPermissionCommandResult(savedUserPermission, permission, tenantMember, now);
    }

    /// <summary>
    /// 更新用户直授权限
    /// </summary>
    /// <param name="command">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    public async Task<UserPermissionCommandResult> UpdateUserPermissionAsync(UserPermissionUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUserPermissionUpdateCommand(command);

        var now = DateTimeOffset.UtcNow;
        var userPermission = await GetUserPermissionOrThrowAsync(command.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, "维护直授权限", "平台管理员成员权限必须通过平台运维流程维护。", cancellationToken);
        var permission = await GetGrantablePermissionOrThrowAsync(userPermission.PermissionId, cancellationToken);

        userPermission.PermissionAction = command.PermissionAction;
        userPermission.EffectiveTime = command.EffectiveTime;
        userPermission.ExpirationTime = command.ExpirationTime;
        userPermission.GrantReason = NormalizeNullable(command.GrantReason);
        userPermission.Remark = NormalizeNullable(command.Remark);

        var savedUserPermission = await _userPermissionRepository.UpdateAsync(userPermission, cancellationToken);
        return new UserPermissionCommandResult(savedUserPermission, permission, tenantMember, now);
    }

    /// <summary>
    /// 更新用户直授权限状态
    /// </summary>
    /// <param name="command">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户直授权限详情</returns>
    public async Task<UserPermissionCommandResult> UpdateUserPermissionStatusAsync(UserPermissionStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户直授权限绑定主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var now = DateTimeOffset.UtcNow;
        var userPermission = await GetUserPermissionOrThrowAsync(command.BasicId, cancellationToken);
        var tenantMember = command.Status == ValidityStatus.Valid
            ? await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, "维护直授权限", "平台管理员成员权限必须通过平台运维流程维护。", cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);
        var permission = command.Status == ValidityStatus.Valid
            ? await GetGrantablePermissionOrThrowAsync(userPermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);

        userPermission.Status = command.Status;
        userPermission.Remark = NormalizeNullable(command.Remark);

        var savedUserPermission = await _userPermissionRepository.UpdateAsync(userPermission, cancellationToken);
        return new UserPermissionCommandResult(savedUserPermission, permission, tenantMember, now);
    }

    /// <summary>
    /// 撤销用户直授权限
    /// </summary>
    /// <param name="id">用户直授权限绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteUserPermissionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var userPermission = await GetUserPermissionOrThrowAsync(id, cancellationToken);
        userPermission.Status = ValidityStatus.Invalid;

        _ = await _userPermissionRepository.UpdateAsync(userPermission, cancellationToken);
    }

    #endregion

    #region 用户数据范围

    /// <summary>
    /// 授予用户数据范围
    /// </summary>
    /// <param name="command">授权参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    public async Task<UserDataScopeCommandResult> CreateUserDataScopeAsync(UserDataScopeGrantCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDataScopeGrantCommand(command);

        var now = DateTimeOffset.UtcNow;
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(command.UserId, now, "维护数据范围", "平台管理员成员数据范围必须通过平台运维流程维护。", cancellationToken);
        _ = await GetCustomDataScopeUserOrThrowAsync(command.UserId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(command.DepartmentId, cancellationToken);
        if (await _userDataScopeRepository.AnyAsync(
            scope => scope.UserId == command.UserId && scope.DepartmentId == command.DepartmentId,
            cancellationToken))
        {
            throw new InvalidOperationException("用户数据范围已绑定。");
        }

        var dataScope = new SysUserDataScope
        {
            UserId = command.UserId,
            DepartmentId = command.DepartmentId,
            IncludeChildren = command.IncludeChildren,
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedDataScope = await _userDataScopeRepository.AddAsync(dataScope, cancellationToken);
        return new UserDataScopeCommandResult(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    /// <param name="command">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    public async Task<UserDataScopeCommandResult> UpdateUserDataScopeAsync(UserDataScopeUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDataScopeUpdateCommand(command);

        var now = DateTimeOffset.UtcNow;
        var dataScope = await GetUserDataScopeOrThrowAsync(command.BasicId, cancellationToken);
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(dataScope.UserId, now, "维护数据范围", "平台管理员成员数据范围必须通过平台运维流程维护。", cancellationToken);
        _ = await GetCustomDataScopeUserOrThrowAsync(dataScope.UserId, cancellationToken);
        var department = await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken);

        dataScope.IncludeChildren = command.IncludeChildren;
        dataScope.Remark = NormalizeNullable(command.Remark);

        var savedDataScope = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return new UserDataScopeCommandResult(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    /// <param name="command">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    public async Task<UserDataScopeCommandResult> UpdateUserDataScopeStatusAsync(UserDataScopeStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户数据范围绑定主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var now = DateTimeOffset.UtcNow;
        var dataScope = await GetUserDataScopeOrThrowAsync(command.BasicId, cancellationToken);
        var tenantMember = command.Status == ValidityStatus.Valid
            ? await GetAssignableTenantMemberOrThrowAsync(dataScope.UserId, now, "维护数据范围", "平台管理员成员数据范围必须通过平台运维流程维护。", cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(dataScope.UserId, cancellationToken);
        var department = command.Status == ValidityStatus.Valid
            ? await GetEnabledDepartmentOrThrowAsync(dataScope.DepartmentId, cancellationToken)
            : await GetDepartmentOrDefaultAsync(dataScope.DepartmentId, cancellationToken);

        if (command.Status == ValidityStatus.Valid)
        {
            _ = await GetCustomDataScopeUserOrThrowAsync(dataScope.UserId, cancellationToken);
        }

        dataScope.Status = command.Status;
        dataScope.Remark = NormalizeNullable(command.Remark);

        var savedDataScope = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
        return new UserDataScopeCommandResult(savedDataScope, department, tenantMember);
    }

    /// <summary>
    /// 撤销用户数据范围
    /// </summary>
    /// <param name="id">用户数据范围绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteUserDataScopeAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var dataScope = await GetUserDataScopeOrThrowAsync(id, cancellationToken);
        dataScope.Status = ValidityStatus.Invalid;

        _ = await _userDataScopeRepository.UpdateAsync(dataScope, cancellationToken);
    }

    #endregion

    #region 用户部门

    /// <summary>
    /// 分配用户部门归属
    /// </summary>
    /// <param name="command">分配参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    public async Task<UserDepartmentCommandResult> CreateUserDepartmentAsync(UserDepartmentAssignCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDepartmentAssignCommand(command);

        var now = DateTimeOffset.UtcNow;
        _ = await GetAssignableTenantMemberOrThrowAsync(command.UserId, now, "分配部门", "平台管理员成员部门归属必须通过平台运维流程维护。", cancellationToken);
        var department = await GetAssignableDepartmentOrThrowAsync(command.DepartmentId, cancellationToken);
        var shouldBeMain = command.IsMain || !await HasValidDepartmentAsync(command.UserId, cancellationToken);

        var userDepartment = await _userDepartmentRepository.GetFirstAsync(
            relation => relation.UserId == command.UserId && relation.DepartmentId == command.DepartmentId,
            cancellationToken);
        if (userDepartment is not null && userDepartment.Status == ValidityStatus.Valid)
        {
            throw new InvalidOperationException("用户部门归属已存在。");
        }

        if (shouldBeMain)
        {
            await ClearOtherMainDepartmentsAsync(command.UserId, userDepartment?.BasicId, cancellationToken);
        }

        if (userDepartment is null)
        {
            userDepartment = new SysUserDepartment
            {
                UserId = command.UserId,
                DepartmentId = command.DepartmentId,
                PositionId = NormalizePositionId(command.PositionId),
                JobNumber = NormalizeNullable(command.JobNumber),
                JobLevel = NormalizeNullable(command.JobLevel),
                JoinTime = command.JoinTime,
                IsMain = shouldBeMain,
                Status = ValidityStatus.Valid,
                Remark = NormalizeNullable(command.Remark)
            };

            var savedUserDepartment = await _userDepartmentRepository.AddAsync(userDepartment, cancellationToken);
            return new UserDepartmentCommandResult(savedUserDepartment, department);
        }

        userDepartment.PositionId = NormalizePositionId(command.PositionId);
        userDepartment.JobNumber = NormalizeNullable(command.JobNumber);
        userDepartment.JobLevel = NormalizeNullable(command.JobLevel);
        userDepartment.JoinTime = command.JoinTime;
        userDepartment.IsMain = shouldBeMain;
        userDepartment.Status = ValidityStatus.Valid;
        userDepartment.Remark = NormalizeNullable(command.Remark);

        var restoredUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        return new UserDepartmentCommandResult(restoredUserDepartment, department);
    }

    /// <summary>
    /// 更新用户部门归属
    /// </summary>
    /// <param name="command">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    public async Task<UserDepartmentCommandResult> UpdateUserDepartmentAsync(UserDepartmentUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDepartmentUpdateCommand(command);

        var now = DateTimeOffset.UtcNow;
        var userDepartment = await GetUserDepartmentOrThrowAsync(command.BasicId, cancellationToken);
        if (userDepartment.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效用户部门归属不能更新。");
        }

        _ = await GetAssignableTenantMemberOrThrowAsync(userDepartment.UserId, now, "分配部门", "平台管理员成员部门归属必须通过平台运维流程维护。", cancellationToken);
        var department = await GetAssignableDepartmentOrThrowAsync(userDepartment.DepartmentId, cancellationToken);
        if (command.IsMain)
        {
            await ClearOtherMainDepartmentsAsync(userDepartment.UserId, userDepartment.BasicId, cancellationToken);
        }

        userDepartment.PositionId = NormalizePositionId(command.PositionId);
        userDepartment.JobNumber = NormalizeNullable(command.JobNumber);
        userDepartment.JobLevel = NormalizeNullable(command.JobLevel);
        userDepartment.JoinTime = command.JoinTime;
        userDepartment.IsMain = command.IsMain;
        userDepartment.Remark = NormalizeNullable(command.Remark);

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        return new UserDepartmentCommandResult(savedUserDepartment, department);
    }

    /// <summary>
    /// 更新用户部门归属状态
    /// </summary>
    /// <param name="command">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    public async Task<UserDepartmentCommandResult> UpdateUserDepartmentStatusAsync(UserDepartmentStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户部门归属主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var now = DateTimeOffset.UtcNow;
        var userDepartment = await GetUserDepartmentOrThrowAsync(command.BasicId, cancellationToken);
        var department = command.Status == ValidityStatus.Valid
            ? await GetAssignableDepartmentOrThrowAsync(userDepartment.DepartmentId, cancellationToken)
            : await _departmentRepository.GetByIdAsync(userDepartment.DepartmentId, cancellationToken);

        if (command.Status == ValidityStatus.Valid)
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

        userDepartment.Status = command.Status;
        userDepartment.Remark = NormalizeNullable(command.Remark);

        var savedUserDepartment = await _userDepartmentRepository.UpdateAsync(userDepartment, cancellationToken);
        if (command.Status != ValidityStatus.Valid)
        {
            await PromoteMainDepartmentIfNeededAsync(savedUserDepartment.UserId, savedUserDepartment.BasicId, cancellationToken);
        }

        return new UserDepartmentCommandResult(savedUserDepartment, department);
    }

    /// <summary>
    /// 撤销用户部门归属
    /// </summary>
    /// <param name="id">用户部门归属主键</param>
    /// <param name="cancellationToken">取消令牌</param>
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

    #region 用户会话

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    /// <param name="command">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话详情</returns>
    public async Task<UserSessionCommandResult> RevokeUserSessionAsync(UserSessionRevokeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRevokeCommand(command.BasicId, command.Reason, "会话主键必须大于 0。");

        var session = await GetSessionOrThrowAsync(command.BasicId, cancellationToken);
        var user = await _userRepository.GetByIdAsync(session.UserId, cancellationToken);
        UserSessionRevokedDomainEvent? domainEvent = null;
        if (session.Status != SessionStatus.Revoked)
        {
            var reason = command.Reason.Trim();
            RevokeSession(session, reason, DateTimeOffset.UtcNow);
            session = await _userSessionRepository.UpdateAsync(session, cancellationToken);
            domainEvent = BuildSessionRevokedEvent(session, revokeAllUserSessions: false, command.OperatorUserId, reason);
        }

        return new UserSessionCommandResult(session, user, DateTimeOffset.UtcNow, domainEvent);
    }

    /// <summary>
    /// 撤销用户全部会话
    /// </summary>
    /// <param name="command">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销会话数量</returns>
    public async Task<UserSessionsRevokeResult> RevokeUserSessionsAsync(UserSessionsRevokeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRevokeCommand(command.UserId, command.Reason, "用户主键必须大于 0。");

        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");
        var sessions = await _userSessionRepository.GetListAsync(
            session => session.UserId == user.BasicId && session.Status != SessionStatus.Revoked,
            cancellationToken);

        if (sessions.Count == 0)
        {
            return new UserSessionsRevokeResult(0, null);
        }

        var now = DateTimeOffset.UtcNow;
        var reason = command.Reason.Trim();
        foreach (var session in sessions)
        {
            RevokeSession(session, reason, now);
        }

        _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        return new UserSessionsRevokeResult(
            sessions.Count,
            BuildUserSessionsRevokedEvent(user, sessions[0].TenantId, command.OperatorUserId, reason));
    }

    #endregion

    // ================================================================
    // #region 私有辅助方法 (统一管理)
    // ================================================================

    #region 私有辅助方法

    // ---- 用户核心辅助 ----

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

    /// <summary>
    /// 撤销会话
    /// </summary>
    private static void RevokeSession(SysUserSession session, string reason, DateTimeOffset now)
    {
        session.Status = SessionStatus.Revoked;
        session.RevokedTime = now;
        session.RevokedReason = reason;
        session.LogoutTime ??= now;
        session.LastActivityTime = now;
    }

    /// <summary>
    /// 创建单会话撤销事件
    /// </summary>
    private static UserSessionRevokedDomainEvent BuildSessionRevokedEvent(
        SysUserSession session,
        bool revokeAllUserSessions,
        long? operatorUserId,
        string reason)
    {
        return new UserSessionRevokedDomainEvent(
            session.TenantId,
            session.UserId,
            session.BasicId,
            session.UserSessionId,
            session.CurrentAccessTokenJti,
            revokeAllUserSessions,
            operatorUserId,
            reason);
    }

    /// <summary>
    /// 创建用户全部会话撤销事件
    /// </summary>
    private static UserSessionRevokedDomainEvent BuildUserSessionsRevokedEvent(
        SysUser user,
        long sessionTenantKey,
        long? operatorUserId,
        string reason)
    {
        return new UserSessionRevokedDomainEvent(
            sessionTenantKey,
            user.BasicId,
            sessionId: null,
            userSessionId: null,
            accessTokenJti: null,
            revokeAllUserSessions: true,
            operatorUserId,
            reason);
    }

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateCommand(UserCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.UserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.InitialPassword);

        ValidateRequiredLength(command.UserName, 50, nameof(command.UserName), "用户名不能超过 50 个字符。");
        ValidateOptionalLength(command.RealName, 50, nameof(command.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(command.NickName, 50, nameof(command.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(command.Avatar, 500, nameof(command.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(command.Email, 100, nameof(command.Email), "邮箱不能超过 100 个字符。");
        ValidateOptionalLength(command.Phone, 20, nameof(command.Phone), "手机号不能超过 20 个字符。");
        ValidateOptionalLength(command.TimeZone, 50, nameof(command.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(command.Language, 10, nameof(command.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(command.Country, 50, nameof(command.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(command.DisplayName, 100, nameof(command.DisplayName), "租户内显示名不能超过 100 个字符。");
        ValidateOptionalLength(command.InviteRemark, 500, nameof(command.InviteRemark), "邀请备注不能超过 500 个字符。");
        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
        ValidateEnum(command.Gender, nameof(command.Gender));
        ValidateEnum(command.Status, nameof(command.Status));
        ValidateEnum(command.MemberType, nameof(command.MemberType));
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime, "成员");
        EnsureMemberTypeCanBeCreated(command.MemberType);
    }

    // ---- 共享校验 ----
    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateCommand(UserUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        ValidateOptionalLength(command.RealName, 50, nameof(command.RealName), "真实姓名不能超过 50 个字符。");
        ValidateOptionalLength(command.NickName, 50, nameof(command.NickName), "昵称不能超过 50 个字符。");
        ValidateOptionalLength(command.Avatar, 500, nameof(command.Avatar), "头像不能超过 500 个字符。");
        ValidateOptionalLength(command.Email, 100, nameof(command.Email), "邮箱不能超过 100 个字符。");
        ValidateOptionalLength(command.Phone, 20, nameof(command.Phone), "手机号不能超过 20 个字符。");
        ValidateOptionalLength(command.TimeZone, 50, nameof(command.TimeZone), "时区不能超过 50 个字符。");
        ValidateOptionalLength(command.Language, 10, nameof(command.Language), "语言不能超过 10 个字符。");
        ValidateOptionalLength(command.Country, 50, nameof(command.Country), "国家/地区不能超过 50 个字符。");
        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
        ValidateEnum(command.Gender, nameof(command.Gender));
    }

    /// <summary>
    /// 构建密码黑名单 (UserCreateCommand 重载)
    /// </summary>
    private static List<string> BuildPasswordBlacklist(UserCreateCommand command)
    {
        return
        [
            .. new[]
            {
                command.UserName,
                command.RealName,
                command.NickName,
                command.Email,
                command.Phone
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
    private static void ValidatePasswordResetCommand(UserPasswordResetCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(command.NewPassword);
        if (command.PasswordExpirationTime.HasValue && command.PasswordExpirationTime.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "密码过期时间必须晚于当前时间。");
        }

        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验锁定参数
    /// </summary>
    private static void ValidateLockCommand(UserLockChangeCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        if (command.IsLocked && command.LockoutEndTime.HasValue && command.LockoutEndTime.Value <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "锁定结束时间必须晚于当前时间。");
        }

        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验登录策略参数
    /// </summary>
    private static void ValidateLoginPolicyCommand(UserLoginPolicyUpdateCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        if (command.MaxLoginDevices < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "最大登录设备数不能小于 0。");
        }

        if (!command.AllowMultiLogin && command.MaxLoginDevices == 0)
        {
            throw new InvalidOperationException("禁用多端登录时最大登录设备数必须大于 0。");
        }

        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验用户角色授权参数
    /// </summary>
    private static void ValidateUserRoleGrantCommand(UserRoleGrantCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        if (command.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime, "用户角色");
    }

    /// <summary>
    /// 校验用户角色更新参数
    /// </summary>
    private static void ValidateUserRoleUpdateCommand(UserRoleUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户角色绑定主键必须大于 0。");
        }

        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime, "用户角色");
    }

    /// <summary>
    /// 校验用户直授权限授权参数
    /// </summary>
    private static void ValidateUserPermissionGrantCommand(UserPermissionGrantCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        if (command.PermissionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "权限主键必须大于 0。");
        }

        ValidateEnum(command.PermissionAction, nameof(command.PermissionAction));
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime, "用户直授权限");
    }

    /// <summary>
    /// 校验用户直授权限更新参数
    /// </summary>
    private static void ValidateUserPermissionUpdateCommand(UserPermissionUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户直授权限绑定主键必须大于 0。");
        }

        ValidateEnum(command.PermissionAction, nameof(command.PermissionAction));
        ValidateEffectivePeriod(command.EffectiveTime, command.ExpirationTime, "用户直授权限");
    }

    /// <summary>
    /// 校验用户数据范围授权参数
    /// </summary>
    private static void ValidateDataScopeGrantCommand(UserDataScopeGrantCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        if (command.DepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "部门主键必须大于 0。");
        }
    }

    /// <summary>
    /// 校验用户数据范围更新参数
    /// </summary>
    private static void ValidateDataScopeUpdateCommand(UserDataScopeUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户数据范围绑定主键必须大于 0。");
        }
    }

    /// <summary>
    /// 校验用户部门分配参数
    /// </summary>
    private static void ValidateDepartmentAssignCommand(UserDepartmentAssignCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        if (command.DepartmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "部门主键必须大于 0。");
        }

        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验用户部门更新参数
    /// </summary>
    private static void ValidateDepartmentUpdateCommand(UserDepartmentUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户部门归属主键必须大于 0。");
        }

        ValidateOptionalLength(command.Remark, 500, nameof(command.Remark), "备注不能超过 500 个字符。");
    }

    /// <summary>
    /// 校验撤销参数
    /// </summary>
    private static void ValidateRevokeCommand(long id, string reason, string idMessage)
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
    /// 归一化岗位主键（0 或负值视为未设置）
    /// </summary>
    private static long? NormalizePositionId(long? positionId)
    {
        return positionId is > 0 ? positionId : null;
    }

    /// <summary>
    /// 创建安全戳
    /// </summary>
    private static string NewSecurityStamp()
    {
        return Guid.NewGuid().ToString("N");
    }

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
    private async Task CreateTenantMembershipAsync(long userId, UserCreateCommand command, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var tenantMember = new SysTenantUser
        {
            UserId = userId,
            MemberType = command.MemberType,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            InvitedBy = command.OperatorUserId,
            InvitedTime = now,
            RespondedTime = now,
            EffectiveTime = command.EffectiveTime,
            ExpirationTime = command.ExpirationTime,
            DisplayName = NormalizeNullable(command.DisplayName),
            InviteRemark = NormalizeNullable(command.InviteRemark),
            Status = ValidityStatus.Valid,
            Remark = NormalizeNullable(command.Remark)
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
    /// 校验邮箱全平台唯一（邮箱为登录身份标识；为空跳过）
    /// </summary>
    private async Task EnsureEmailUniqueAsync(string? email, long? excludeUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        if (await _userRepository.ExistsEmailGloballyAsync(email, excludeUserId, cancellationToken))
        {
            throw new InvalidOperationException("邮箱已被其他账号使用。");
        }
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
    /// 校验用户能否被重置密码
    /// </summary>
    private async Task EnsureUserCanBeResetAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能通过用户安全服务重置密码。");
        }

        var membership = await _tenantUserRepository.GetMembershipAsync(user.BasicId, cancellationToken);
        if (membership?.MemberType == TenantMemberType.Owner)
        {
            throw new InvalidOperationException("租户所有者账号不能通过用户安全服务重置密码。");
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

        if (role.RoleType == RoleType.System && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("系统角色仅平台运维态可分配，请切换到平台运维后操作。");
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
    /// 获取数据权限范围覆盖为自定义的用户，不满足规则时抛出异常（与角色侧 GetCustomDataScopeRoleOrThrowAsync 对称）
    /// </summary>
    private async Task<SysUser> GetCustomDataScopeUserOrThrowAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("用户不存在。");

        if (user.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用用户不能维护数据范围。");
        }

        if (user.DataScopeOverride != DataPermissionScope.Custom)
        {
            throw new InvalidOperationException("只有数据权限范围覆盖为自定义（DataScopeOverride=Custom）的用户才能维护部门数据范围。");
        }

        return user;
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
        // 仅下推列谓词（UserId + IsMain）；排除项含可空闭包的 OR 一旦下推会被 SqlSugar 误译为非法 SQL（PostgreSQL 42601），故在内存中排除
        var mainDepartments = await _userDepartmentRepository.GetListAsync(
            relation => relation.UserId == userId && relation.IsMain,
            cancellationToken);
        var targets = excludeId.HasValue
            ? mainDepartments.Where(relation => relation.BasicId != excludeId.Value).ToList()
            : mainDepartments.ToList();
        if (targets.Count == 0)
        {
            return;
        }

        foreach (var mainDepartment in targets)
        {
            mainDepartment.IsMain = false;
        }

        await _userDepartmentRepository.UpdateRangeAsync(targets, cancellationToken);
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

        if (tenantMember.MemberType == TenantMemberType.PlatformAdmin && !_currentTenant.IsPlatformOperation())
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

    /// <summary>
    /// 校验密码策略 (UserCreateCommand 重载)
    /// </summary>
    private async Task EnsurePasswordMeetsPolicyAsync(UserCreateCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var blacklist = BuildPasswordBlacklist(command);
        var result = await _authenticationService.ValidatePasswordStrengthAsync(command.InitialPassword, blacklist);
        if (result.IsValid)
        {
            return;
        }

        var errors = result.Errors.Count > 0 ? string.Join("；", result.Errors) : result.Message;
        throw new InvalidOperationException($"初始密码不符合安全要求：{errors}");
    }

    #endregion
}
