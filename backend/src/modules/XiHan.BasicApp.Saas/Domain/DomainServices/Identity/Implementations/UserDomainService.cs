// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authentication.Users;
using XiHan.Framework.Domain.Repositories;
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

    /// <summary>
    /// 角色仓储
    /// </summary>
    private readonly IRoleRepository _roleRepository;

    /// <summary>
    /// 用户直授权限仓储
    /// </summary>
    private readonly IUserPermissionRepository _userPermissionRepository;

    /// <summary>
    /// 权限仓储
    /// </summary>
    private readonly IPermissionRepository _permissionRepository;

    /// <summary>
    /// 用户数据范围仓储
    /// </summary>
    private readonly IUserDataScopeRepository _userDataScopeRepository;

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
    /// 密码历史领域服务
    /// </summary>
    private readonly IPasswordHistoryDomainService _passwordHistoryDomainService;

    private readonly IConstraintRuleEnforcementDomainService _constraintRuleEnforcementDomainService;

    /// <summary>
    /// 租户配额领域服务
    /// </summary>
    private readonly ITenantQuotaDomainService _tenantQuotaDomainService;

    private readonly ILogger<UserDomainService> _logger;

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
        ICurrentTenant currentTenant,
        IPasswordHistoryDomainService passwordHistoryDomainService,
        IConstraintRuleEnforcementDomainService constraintRuleEnforcementDomainService,
        ITenantQuotaDomainService tenantQuotaDomainService,
        ILogger<UserDomainService> logger)
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
        _passwordHistoryDomainService = passwordHistoryDomainService;
        _constraintRuleEnforcementDomainService = constraintRuleEnforcementDomainService;
        _tenantQuotaDomainService = tenantQuotaDomainService;
        _logger = logger;
    }

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

        // 平台里建的是平台账号：平台没有成员关系，也没有席位
        var isPlatform = _currentTenant.IsPlatformOperation();

        // 席位配额放在轻量校验之后：用户名/邮箱冲突这类错误先短路，避免无谓的用量统计查询。
        // 此处不必排除 PlatformAdmin —— 本流程只能创建普通成员，Owner 与 PlatformAdmin
        // 在上面的 ValidateCreateCommand → EnsureMemberTypeCanBeCreated 已被拒；
        // 「平台管理员不占席位」由统计侧的 CountActiveMembersByTenantIdsAsync 保证。
        if (!isPlatform)
        {
            await _tenantQuotaDomainService.EnsureSeatQuotaAsync(1, cancellationToken);
        }

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
            Country = NormalizeNullable(command.Country),
            IsSystemAccount = false,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedUser = await _userRepository.AddAsync(user, cancellationToken);
        await CreateUserSecurityAsync(savedUser, command.InitialPassword, now, command.Remark, cancellationToken);
        if (!isPlatform)
        {
            await CreateTenantMembershipAsync(savedUser.BasicId, command, now, cancellationToken);
        }

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
        var memberships = await EnsureUserCanBeDeletedAsync(user, cancellationToken);

        // 账号没了，它在每个租户里的成员关系都作废；逐租户切入写，不在当前上下文代写别的租户
        var now = DateTimeOffset.UtcNow;
        foreach (var membership in memberships.Where(item => item.InviteStatus != TenantMemberInviteStatus.Revoked || item.Status != ValidityStatus.Invalid))
        {
            membership.InviteStatus = TenantMemberInviteStatus.Revoked;
            membership.Status = ValidityStatus.Invalid;
            membership.RespondedTime ??= now;
            using (_currentTenant.Change(membership.TenantId))
            {
                _ = await _tenantUserRepository.UpdateAsync(membership, cancellationToken);
            }
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
        await _passwordHistoryDomainService.EnsureNotReusedAsync(user.BasicId, command.NewPassword, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        security.Password = _passwordHasher.HashPassword(command.NewPassword);
        security.LastPasswordChangeTime = now;
        security.PasswordExpirationTime = command.PasswordExpirationTime;
        security.FailedLoginAttempts = 0;
        security.LastFailedLoginTime = null;
        security.SecurityStamp = NewSecurityStamp();
        security.Remark = NormalizeNullable(command.Remark);

        var savedSecurity = await _userSecurityRepository.UpdateAsync(security, cancellationToken);
        await _passwordHistoryDomainService.RecordAsync(user, security.Password, now, cancellationToken);
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
    /// 批量变更用户角色（一次性提交授予与撤销）
    /// </summary>
    /// <param name="command">批量变更命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本次实际发生变化的角色</returns>
    public async Task<UserRoleBatchUpdateResult> BatchUpdateUserRolesAsync(UserRoleBatchUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        var grantRoleIds = command.GrantRoleIds.Where(id => id > 0).Distinct().ToList();
        var revokeIds = command.RevokeUserRoleIds.Where(id => id > 0).Distinct().ToList();
        if (grantRoleIds.Count == 0 && revokeIds.Count == 0)
        {
            return new UserRoleBatchUpdateResult([], []);
        }

        var now = DateTimeOffset.UtcNow;
        _ = await GetAssignableTenantMemberOrThrowAsync(command.UserId, now, "分配角色", cancellationToken);

        // 撤销只认本用户名下、仍为有效状态的记录：别人的记录主键混进来不会被改动，已失效的也不重复记账。
        // 同一角色本次既撤又授时以授予为准，不撤销
        var grantRoleIdSet = grantRoleIds.ToHashSet();
        var revoking = revokeIds.Count == 0
            ? []
            : (await _userRoleRepository.GetListAsync(
                userRole => revokeIds.Contains(userRole.BasicId)
                    && userRole.UserId == command.UserId
                    && userRole.Status == ValidityStatus.Valid,
                cancellationToken))
                .Where(userRole => !grantRoleIdSet.Contains(userRole.RoleId))
                .ToList();

        // 授予前逐个校验角色可分配，规则与单条读取共用 EnsureAssignableRole
        var roleMap = grantRoleIds.Count == 0
            ? []
            : (await _roleRepository.GetListAsync(
                role => grantRoleIds.Contains(role.BasicId), cancellationToken))
                .ToDictionary(role => role.BasicId);
        foreach (var roleId in grantRoleIds)
        {
            EnsureAssignableRole(roleMap.GetValueOrDefault(roleId));
        }

        // 撤销只置无效不删行（唯一索引按 租户×用户×角色），同一绑定的历史行会留在库里，命中即就地复用
        var existingMap = grantRoleIds.Count == 0
            ? []
            : (await _userRoleRepository.GetListAsync(
                userRole => userRole.UserId == command.UserId && grantRoleIds.Contains(userRole.RoleId),
                cancellationToken)).ToDictionary(userRole => userRole.RoleId);

        var updating = new List<SysUserRole>();
        var adding = new List<SysUserRole>();
        var grantedRoleIds = new List<long>();
        var joiningRoleIds = new List<long>();
        foreach (var roleId in grantRoleIds)
        {
            if (!existingMap.TryGetValue(roleId, out var userRole))
            {
                adding.Add(new SysUserRole
                {
                    UserId = command.UserId,
                    RoleId = roleId,
                    Status = ValidityStatus.Valid
                });
                grantedRoleIds.Add(roleId);
                joiningRoleIds.Add(roleId);
                continue;
            }

            // 已经生效的绑定原样保留，不算变更
            if (IsEffective(userRole.Status, userRole.EffectiveTime, userRole.ExpirationTime, now))
            {
                continue;
            }

            // 尚未生效的预约已经占着名额，复用它不算新加入
            if (!OccupiesSeat(userRole, now))
            {
                joiningRoleIds.Add(roleId);
            }

            // 复用的历史行多半是失效或已过期的：只改状态不动时间窗，保存后仍不生效，等于伪成功。
            // 这里的授予语义是「从现在起生效」，所以把时间窗一并清空
            userRole.Status = ValidityStatus.Valid;
            userRole.EffectiveTime = null;
            userRole.ExpirationTime = null;
            updating.Add(userRole);
            grantedRoleIds.Add(roleId);
        }

        // SSD 执法：以「现有有效角色 − 本次撤销 + 本次授予」这一最终角色集评估静态职责分离约束（含继承链展开）
        if (grantedRoleIds.Count > 0)
        {
            var revokedRoleIds = revoking.Select(userRole => userRole.RoleId).ToHashSet();
            var finalRoleIds = (await _userRoleRepository.GetValidByUserIdAsync(command.UserId, now, cancellationToken))
                .Select(userRole => userRole.RoleId)
                .Where(roleId => !revokedRoleIds.Contains(roleId))
                .Concat(grantedRoleIds)
                .Distinct();
            await EnsureNoSoDConflictAsync(finalRoleIds, cancellationToken);
        }

        foreach (var roleId in joiningRoleIds)
        {
            await EnsureRoleCapacityAsync(roleMap[roleId], joining: 1, leaving: 0, now, cancellationToken);
        }

        if (revoking.Count > 0)
        {
            foreach (var userRole in revoking)
            {
                userRole.Status = ValidityStatus.Invalid;
            }

            _ = await _userRoleRepository.UpdateRangeAsync(revoking, cancellationToken);
        }

        if (updating.Count > 0)
        {
            _ = await _userRoleRepository.UpdateRangeAsync(updating, cancellationToken);
        }

        if (adding.Count > 0)
        {
            _ = await _userRoleRepository.AddRangeAsync(adding, cancellationToken);
        }

        return new UserRoleBatchUpdateResult(grantedRoleIds, [.. revoking.Select(userRole => userRole.RoleId)]);
    }

    /// <summary>
    /// 批量变更角色成员（以角色为中心，一次性提交加入与移出）
    /// </summary>
    /// <remarks>
    /// 与按用户批量改角色同一套规则：成员须是本租户可授权的成员，角色须可分配，加入后不违反职责分离、不超角色成员上限。
    /// 移出只置失效不删行，同一 用户×角色 的历史行命中即就地复用、从现在起生效；同一成员本次既移出又加入时以加入为准。
    /// </remarks>
    /// <param name="command">批量变更命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本次实际加入与移出的成员</returns>
    public async Task<RoleMemberBatchUpdateResult> BatchUpdateRoleMembersAsync(RoleMemberBatchUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.RoleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "角色主键必须大于 0。");
        }

        var grantUserIds = command.GrantUserIds.Where(id => id > 0).Distinct().ToList();
        var revokeIds = command.RevokeUserRoleIds.Where(id => id > 0).Distinct().ToList();
        if (grantUserIds.Count == 0 && revokeIds.Count == 0)
        {
            return new RoleMemberBatchUpdateResult([], []);
        }

        var now = DateTimeOffset.UtcNow;
        var role = await _roleRepository.GetByIdAsync(command.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("角色不存在。");
        if (grantUserIds.Count > 0)
        {
            EnsureAssignableRole(role);
        }

        foreach (var userId in grantUserIds)
        {
            _ = await GetAssignableTenantMemberOrThrowAsync(userId, now, "分配角色", cancellationToken);
        }

        var grantUserIdSet = grantUserIds.ToHashSet();
        var revoking = revokeIds.Count == 0
            ? []
            : (await _userRoleRepository.GetListAsync(
                userRole => revokeIds.Contains(userRole.BasicId)
                    && userRole.RoleId == role.BasicId
                    && userRole.Status == ValidityStatus.Valid,
                cancellationToken))
                .Where(userRole => !grantUserIdSet.Contains(userRole.UserId))
                .ToList();

        var existingMap = grantUserIds.Count == 0
            ? []
            : (await _userRoleRepository.GetListAsync(
                userRole => userRole.RoleId == role.BasicId && grantUserIds.Contains(userRole.UserId),
                cancellationToken)).ToDictionary(userRole => userRole.UserId);

        var updating = new List<SysUserRole>();
        var adding = new List<SysUserRole>();
        var grantedUserIds = new List<long>();
        var joining = 0;
        foreach (var userId in grantUserIds)
        {
            if (!existingMap.TryGetValue(userId, out var userRole))
            {
                adding.Add(new SysUserRole
                {
                    UserId = userId,
                    RoleId = role.BasicId,
                    Status = ValidityStatus.Valid
                });
                grantedUserIds.Add(userId);
                joining++;
                continue;
            }

            if (IsEffective(userRole.Status, userRole.EffectiveTime, userRole.ExpirationTime, now))
            {
                continue;
            }

            if (!OccupiesSeat(userRole, now))
            {
                joining++;
            }

            // 授予语义是「从现在起生效」：复用的历史行一并清空时间窗
            userRole.Status = ValidityStatus.Valid;
            userRole.EffectiveTime = null;
            userRole.ExpirationTime = null;
            updating.Add(userRole);
            grantedUserIds.Add(userId);
        }

        await EnsureRoleCapacityAsync(role, joining, revoking.Count(userRole => OccupiesSeat(userRole, now)), now, cancellationToken);

        // SSD 执法：每个加入的成员以「现有有效角色 + 本角色」评估静态职责分离约束
        foreach (var userId in grantedUserIds)
        {
            var finalRoleIds = (await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken))
                .Select(userRole => userRole.RoleId)
                .Append(role.BasicId)
                .Distinct();
            await EnsureNoSoDConflictAsync(finalRoleIds, cancellationToken);
        }

        if (revoking.Count > 0)
        {
            foreach (var userRole in revoking)
            {
                userRole.Status = ValidityStatus.Invalid;
            }

            _ = await _userRoleRepository.UpdateRangeAsync(revoking, cancellationToken);
        }

        if (updating.Count > 0)
        {
            _ = await _userRoleRepository.UpdateRangeAsync(updating, cancellationToken);
        }

        if (adding.Count > 0)
        {
            _ = await _userRoleRepository.AddRangeAsync(adding, cancellationToken);
        }

        return new RoleMemberBatchUpdateResult(grantedUserIds, [.. revoking.Select(userRole => userRole.UserId)]);
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
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, "分配角色", cancellationToken);
        var role = await GetAssignableRoleOrThrowAsync(userRole.RoleId, cancellationToken);
        var occupiedBefore = OccupiesSeat(userRole, now);

        userRole.EffectiveTime = command.EffectiveTime;
        userRole.ExpirationTime = command.ExpirationTime;
        userRole.GrantReason = NormalizeNullable(command.GrantReason);
        userRole.Remark = NormalizeNullable(command.Remark);

        if (!occupiedBefore && OccupiesSeat(userRole, now))
        {
            await EnsureRoleBindingCanJoinAsync(userRole.UserId, role, now, cancellationToken);
        }

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
            ? await GetAssignableTenantMemberOrThrowAsync(userRole.UserId, now, "分配角色", cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(userRole.UserId, cancellationToken);
        var role = command.Status == ValidityStatus.Valid
            ? await GetAssignableRoleOrThrowAsync(userRole.RoleId, cancellationToken)
            : await _roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
        var occupiedBefore = OccupiesSeat(userRole, now);

        userRole.Status = command.Status;
        userRole.Remark = NormalizeNullable(command.Remark);

        if (role is not null && !occupiedBefore && OccupiesSeat(userRole, now))
        {
            await EnsureRoleBindingCanJoinAsync(userRole.UserId, role, now, cancellationToken);
        }

        var savedUserRole = await _userRoleRepository.UpdateAsync(userRole, cancellationToken);
        return new UserRoleCommandResult(savedUserRole, role, tenantMember, now);
    }

    #endregion

    #region 用户直授权限

    /// <summary>
    /// 批量变更用户直授权限（一次性提交授予与撤销）
    /// </summary>
    /// <param name="command">批量变更命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本次实际发生变化的权限</returns>
    public async Task<UserPermissionBatchUpdateResult> BatchUpdateUserPermissionsAsync(UserPermissionBatchUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        foreach (var grant in command.Grants)
        {
            ValidateEnum(grant.PermissionAction, nameof(grant.PermissionAction));
        }

        // 同一权限重复下发时以最后一条为准
        var grants = command.Grants
            .Where(grant => grant.PermissionId > 0)
            .GroupBy(grant => grant.PermissionId)
            .ToDictionary(group => group.Key, group => group.Last().PermissionAction);
        var revokeIds = command.RevokeUserPermissionIds.Where(id => id > 0).Distinct().ToList();
        if (grants.Count == 0 && revokeIds.Count == 0)
        {
            return new UserPermissionBatchUpdateResult([], [], []);
        }

        var now = DateTimeOffset.UtcNow;
        _ = await GetAssignableTenantMemberOrThrowAsync(command.UserId, now, "维护直授权限", cancellationToken);

        var grantedPermissionIds = new List<long>();
        var deniedPermissionIds = new List<long>();
        var revokedPermissionIds = new List<long>();

        // 撤销（逻辑失效）：批量加载 → 置为无效 → 批量更新（单次）
        if (revokeIds.Count > 0)
        {
            var revoking = (await _userPermissionRepository.GetListAsync(
                userPermission => revokeIds.Contains(userPermission.BasicId) && userPermission.UserId == command.UserId,
                cancellationToken)).ToList();
            if (revoking.Count > 0)
            {
                foreach (var userPermission in revoking)
                {
                    userPermission.Status = ValidityStatus.Invalid;
                }

                _ = await _userPermissionRepository.UpdateRangeAsync(revoking, cancellationToken);
                revokedPermissionIds.AddRange(revoking.Select(userPermission => userPermission.PermissionId));
            }
        }

        // 授予：批量校验权限可授予 → 命中历史行就地改写、否则新增（各一次批量写）
        if (grants.Count > 0)
        {
            var permissionIds = grants.Keys.ToList();
            var permissions = await _permissionRepository.GetListAsync(
                permission => permissionIds.Contains(permission.BasicId), cancellationToken);
            var permissionMap = permissions.ToDictionary(permission => permission.BasicId);
            foreach (var permissionId in permissionIds)
            {
                if (!permissionMap.TryGetValue(permissionId, out var permission))
                {
                    throw new InvalidOperationException("权限不存在。");
                }

                if (permission.Status != EnableStatus.Enabled)
                {
                    throw new InvalidOperationException("停用权限不能直授给用户。");
                }
            }

            // 撤销只置无效不删行，同一 用户×权限 的历史行会留在库里，命中即就地改写
            var existing = (await _userPermissionRepository.GetListAsync(
                userPermission => userPermission.UserId == command.UserId && permissionIds.Contains(userPermission.PermissionId),
                cancellationToken)).ToList();
            var existingMap = existing.ToDictionary(userPermission => userPermission.PermissionId);

            var updating = new List<SysUserPermission>();
            var adding = new List<SysUserPermission>();
            foreach (var (permissionId, action) in grants)
            {
                if (existingMap.TryGetValue(permissionId, out var userPermission))
                {
                    // 复用的历史行若此刻不生效（已撤销或已过期），只改状态不动时间窗，保存后仍不生效，等于伪成功。
                    // 授予语义是「从现在起生效」；此刻已生效的行只改动作，保留原配的时间窗
                    if (!IsEffective(userPermission.Status, userPermission.EffectiveTime, userPermission.ExpirationTime, now))
                    {
                        userPermission.EffectiveTime = null;
                        userPermission.ExpirationTime = null;
                    }

                    userPermission.PermissionAction = action;
                    userPermission.Status = ValidityStatus.Valid;
                    updating.Add(userPermission);
                }
                else
                {
                    adding.Add(new SysUserPermission
                    {
                        UserId = command.UserId,
                        PermissionId = permissionId,
                        PermissionAction = action,
                        Status = ValidityStatus.Valid
                    });
                }

                if (action == PermissionAction.Deny)
                {
                    deniedPermissionIds.Add(permissionId);
                }
                else
                {
                    grantedPermissionIds.Add(permissionId);
                }
            }

            if (updating.Count > 0)
            {
                _ = await _userPermissionRepository.UpdateRangeAsync(updating, cancellationToken);
            }

            if (adding.Count > 0)
            {
                _ = await _userPermissionRepository.AddRangeAsync(adding, cancellationToken);
            }
        }

        return new UserPermissionBatchUpdateResult(grantedPermissionIds, deniedPermissionIds, revokedPermissionIds);
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
        var tenantMember = await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, "维护直授权限", cancellationToken);
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
            ? await GetAssignableTenantMemberOrThrowAsync(userPermission.UserId, now, "维护直授权限", cancellationToken)
            : await _tenantUserRepository.GetMembershipAsync(userPermission.UserId, cancellationToken);
        var permission = command.Status == ValidityStatus.Valid
            ? await GetGrantablePermissionOrThrowAsync(userPermission.PermissionId, cancellationToken)
            : await _permissionRepository.GetByIdAsync(userPermission.PermissionId, cancellationToken);

        userPermission.Status = command.Status;
        userPermission.Remark = NormalizeNullable(command.Remark);

        var savedUserPermission = await _userPermissionRepository.UpdateAsync(userPermission, cancellationToken);
        return new UserPermissionCommandResult(savedUserPermission, permission, tenantMember, now);
    }

    #endregion

    #region 用户数据范围

    /// <summary>
    /// 设置成员在本租户的数据范围：覆盖档位与自定义部门一次落地
    /// </summary>
    /// <remarks>
    /// 覆盖挂在成员关系上，同一个人在不同租户各自设置；null 表示跟随角色。数据范围是租户侧概念，平台没有成员关系。
    /// 部门明细与目标比出差量：新部门授予、改了含下级或已撤销的就地复用、目标之外仍有效的撤销（只置无效不删行）；
    /// 档位不是自定义时，已有的部门明细全部撤销。
    /// </remarks>
    /// <param name="command">设置命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>档位是否改变、本次实际变化的部门</returns>
    public async Task<DataScopeSetResult> SetUserDataScopeAsync(UserDataScopeSetCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        if (command.DataScope is { } scope)
        {
            ValidateEnum(scope, nameof(command.DataScope));
        }

        var departments = DataScopeDepartments.Normalize(command.DataScope, command.Departments);
        if (_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("数据范围是租户侧设置：平台没有成员关系，也不施加数据范围。");
        }

        var membership = await GetAssignableTenantMemberOrThrowAsync(command.UserId, DateTimeOffset.UtcNow, "维护数据范围", cancellationToken);
        foreach (var departmentId in departments.Keys)
        {
            _ = await GetEnabledDepartmentOrThrowAsync(departmentId, cancellationToken);
        }

        // 同一 成员×部门 只有一行：撤销只置无效，历史行命中即就地复用
        var rows = (await _userDataScopeRepository.GetListAsync(item => item.UserId == command.UserId, cancellationToken))
            .ToDictionary(item => item.DepartmentId);
        var updating = new List<SysUserDataScope>();
        var adding = new List<SysUserDataScope>();
        var grantedDepartmentIds = new List<long>();
        foreach (var (departmentId, includeChildren) in departments)
        {
            if (!rows.TryGetValue(departmentId, out var dataScope))
            {
                adding.Add(new SysUserDataScope
                {
                    UserId = command.UserId,
                    DepartmentId = departmentId,
                    IncludeChildren = includeChildren,
                    Status = ValidityStatus.Valid
                });
                grantedDepartmentIds.Add(departmentId);
                continue;
            }

            if (dataScope.Status == ValidityStatus.Valid && dataScope.IncludeChildren == includeChildren)
            {
                continue;
            }

            dataScope.IncludeChildren = includeChildren;
            dataScope.Status = ValidityStatus.Valid;
            updating.Add(dataScope);
            grantedDepartmentIds.Add(departmentId);
        }

        var revoking = rows.Values
            .Where(item => item.Status == ValidityStatus.Valid && !departments.ContainsKey(item.DepartmentId))
            .ToList();
        foreach (var dataScope in revoking)
        {
            dataScope.Status = ValidityStatus.Invalid;
        }

        if (revoking.Count > 0 || updating.Count > 0)
        {
            _ = await _userDataScopeRepository.UpdateRangeAsync([.. revoking, .. updating], cancellationToken);
        }

        if (adding.Count > 0)
        {
            _ = await _userDataScopeRepository.AddRangeAsync(adding, cancellationToken);
        }

        var scopeChanged = membership.DataScopeOverride != command.DataScope;
        if (scopeChanged)
        {
            membership.DataScopeOverride = command.DataScope;
            _ = await _tenantUserRepository.UpdateAsync(membership, cancellationToken);
        }

        return new DataScopeSetResult(scopeChanged, grantedDepartmentIds, [.. revoking.Select(item => item.DepartmentId)]);
    }

    #endregion

    #region 用户部门

    /// <summary>
    /// 批量变更用户部门归属（一次性提交分配与撤销）
    /// </summary>
    /// <remarks>
    /// 主部门始终唯一：分配项至多一个标主部门，标了即取代原主部门；本次过后没有有效主部门时，
    /// 依次取留下的有效归属中最早创建的、本次分配项中靠前的接任。
    /// 已有效的归属再次下发只参与主部门调整，岗位、工号等字段走更新接口
    /// </remarks>
    /// <param name="command">批量变更命令</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>本次实际进出的部门</returns>
    public async Task<UserDepartmentBatchUpdateResult> BatchUpdateUserDepartmentsAsync(UserDepartmentBatchUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateDepartmentBatchUpdateCommand(command);

        // 同一部门重复下发时以最后一条为准
        var assigns = command.Assigns
            .GroupBy(assign => assign.DepartmentId)
            .Select(group => group.Last())
            .ToList();
        var revokeIds = command.RevokeUserDepartmentIds.Where(id => id > 0).ToHashSet();
        if (assigns.Count == 0 && revokeIds.Count == 0)
        {
            return new UserDepartmentBatchUpdateResult([], []);
        }

        var mainAssigns = assigns.Where(assign => assign.IsMain).ToList();
        if (mainAssigns.Count > 1)
        {
            throw new InvalidOperationException("一次只能指定一个主部门。");
        }

        if (assigns.Count > 0)
        {
            _ = await GetAssignableTenantMemberOrThrowAsync(command.UserId, DateTimeOffset.UtcNow, "分配部门", cancellationToken);
            foreach (var assign in assigns)
            {
                _ = await GetAssignableDepartmentOrThrowAsync(assign.DepartmentId, cancellationToken);
            }
        }

        // 一个用户的部门归属不多，整批读出后在内存里定终态，主部门的唯一性才能一次算清
        var relations = await _userDepartmentRepository.GetListAsync(
            relation => relation.UserId == command.UserId,
            relation => relation.CreatedTime,
            cancellationToken);
        var relationMap = relations.ToDictionary(relation => relation.DepartmentId);
        var assignedDepartmentIds = assigns.Select(assign => assign.DepartmentId).ToHashSet();
        var updating = new Dictionary<long, SysUserDepartment>();

        // 撤销只认本用户名下、仍为有效状态的记录；同一部门本次既撤又分配时以分配为准
        var revoking = relations
            .Where(relation => revokeIds.Contains(relation.BasicId)
                && relation.Status == ValidityStatus.Valid
                && !assignedDepartmentIds.Contains(relation.DepartmentId))
            .ToList();
        foreach (var relation in revoking)
        {
            relation.Status = ValidityStatus.Invalid;
            updating[relation.BasicId] = relation;
        }

        // 留下的有效归属按创建先后排在前，本次新进的按下发顺序接在后，作为主部门的接任次序
        var candidates = relations.Where(relation => relation.Status == ValidityStatus.Valid).ToList();
        var currentMain = candidates.FirstOrDefault(relation => relation.IsMain);

        // 撤销只置无效不删行，同一 用户×部门 的历史行会留在库里，命中即就地复用
        var adding = new List<SysUserDepartment>();
        var joinedDepartmentIds = new List<long>();
        foreach (var assign in assigns)
        {
            if (!relationMap.TryGetValue(assign.DepartmentId, out var relation))
            {
                relation = new SysUserDepartment
                {
                    UserId = command.UserId,
                    DepartmentId = assign.DepartmentId
                };
                adding.Add(relation);
            }
            else if (relation.Status == ValidityStatus.Valid)
            {
                continue;
            }
            else
            {
                updating[relation.BasicId] = relation;
            }

            relation.PositionId = NormalizePositionId(assign.PositionId);
            relation.JobNumber = NormalizeNullable(assign.JobNumber);
            relation.JobLevel = NormalizeNullable(assign.JobLevel);
            relation.JoinTime = assign.JoinTime;
            relation.Status = ValidityStatus.Valid;
            relation.Remark = NormalizeNullable(assign.Remark);
            candidates.Add(relation);
            joinedDepartmentIds.Add(assign.DepartmentId);
        }

        // 指定的主部门优先，其次沿用现有主部门，都没有时按接任次序取第一个
        var main = mainAssigns.Count == 1
            ? candidates.First(relation => relation.DepartmentId == mainAssigns[0].DepartmentId)
            : currentMain ?? candidates.FirstOrDefault();
        foreach (var relation in relations)
        {
            var isMain = ReferenceEquals(relation, main);
            if (relation.IsMain != isMain)
            {
                relation.IsMain = isMain;
                updating[relation.BasicId] = relation;
            }
        }

        foreach (var relation in adding)
        {
            relation.IsMain = ReferenceEquals(relation, main);
        }

        if (updating.Count > 0)
        {
            _ = await _userDepartmentRepository.UpdateRangeAsync([.. updating.Values], cancellationToken);
        }

        if (adding.Count > 0)
        {
            _ = await _userDepartmentRepository.AddRangeAsync(adding, cancellationToken);
        }

        return new UserDepartmentBatchUpdateResult(joinedDepartmentIds, [.. revoking.Select(relation => relation.DepartmentId)]);
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

        _ = await GetAssignableTenantMemberOrThrowAsync(userDepartment.UserId, now, "分配部门", cancellationToken);
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
            _ = await GetAssignableTenantMemberOrThrowAsync(userDepartment.UserId, now, "分配部门", cancellationToken);
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
        // 会话在当前上下文；账号可能注册在别处（外部成员），按主键跨租户取来展示
        var user = await _userRepository.GetByIdIgnoreTenantAsync(session.UserId, cancellationToken);
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

        // 下线账号的全部会话是账号级操作：只对本上下文注册的账号
        var user = await GetUserOrThrowAsync(command.UserId, cancellationToken);
        // 既取该用户自己的会话，也取由他发起的模仿会话（后者的 UserId 是被模仿者）；
        // 会话行带登录落点的租户戳，同一账号的会话散落在不同租户下，须跨租户取全
        var sessions = (await _userSessionRepository.GetNotRevokedByUserIgnoreTenantAsync(user.BasicId, cancellationToken)).ToList();

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

        // 会话是用户自有行，可能带别的租户戳，显式声明写边界豁免
        using (TenantWriteGuard.Suppress())
        {
            _ = await _userSessionRepository.UpdateRangeAsync(sessions, cancellationToken);
        }

        return new UserSessionsRevokeResult(
            sessions.Count,
            BuildUserSessionsRevokedEvent(user, sessions[0].TenantId, command.OperatorUserId, reason));
    }

    #endregion

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
    /// 校验用户部门批量变更参数
    /// </summary>
    private static void ValidateDepartmentBatchUpdateCommand(UserDepartmentBatchUpdateCommand command)
    {
        if (command.UserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "用户主键必须大于 0。");
        }

        foreach (var assign in command.Assigns)
        {
            if (assign.DepartmentId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(command), "部门主键必须大于 0。");
            }

            ValidateOptionalLength(assign.Remark, 500, nameof(assign.Remark), "备注不能超过 500 个字符。");
        }
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
    private async Task CreateUserSecurityAsync(SysUser user, string password, DateTimeOffset now, string? remark, CancellationToken cancellationToken)
    {
        var passwordHash = _passwordHasher.HashPassword(password);
        var userSecurity = new SysUserSecurity
        {
            UserId = user.BasicId,
            Password = passwordHash,
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
        await _passwordHistoryDomainService.RecordAsync(user, passwordHash, now, cancellationToken);
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
    /// 获取当前上下文注册的账号（身份类操作的入口），不存在时抛出异常
    /// </summary>
    /// <remarks>
    /// 账号严格隔离：只取得到注册在当前上下文的账号。外部成员的身份由其注册地维护，
    /// 在这里只能管理它在本租户的成员关系（角色、权限、部门、数据范围）。
    /// </remarks>
    private async Task<SysUser> GetUserOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "用户主键必须大于 0。");
        }

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is not null)
        {
            return user;
        }

        if (_currentTenant.Id is > 0 && await _tenantUserRepository.GetMembershipAsync(_currentTenant.Id.Value, id, cancellationToken) is not null)
        {
            throw new InvalidOperationException("外部成员的账号由其注册地租户维护，这里只能管理其在本租户的成员关系。");
        }

        throw new InvalidOperationException("用户不存在。");
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

        // 停用账号在所有租户都登录不了：它是任何一个租户的所有者都不能直接停用
        var memberships = await _tenantUserRepository.GetAllByUserIdIgnoreTenantAsync(user.BasicId, cancellationToken);
        if (memberships.Any(IsActiveOwner))
        {
            throw new InvalidOperationException("该账号是租户所有者，不能直接停用；请先转移所有权。");
        }
    }

    /// <summary>
    /// 校验用户能否删除，返回账号在所有租户的成员关系
    /// </summary>
    private async Task<IReadOnlyList<SysTenantUser>> EnsureUserCanBeDeletedAsync(SysUser user, CancellationToken cancellationToken)
    {
        if (user.IsSystemAccount)
        {
            throw new InvalidOperationException("系统内置账号不能删除。");
        }

        // 删除账号会让它在所有租户消失：它是任何一个租户的所有者都不能直接删除
        var memberships = await _tenantUserRepository.GetAllByUserIdIgnoreTenantAsync(user.BasicId, cancellationToken);
        if (memberships.Any(IsActiveOwner))
        {
            throw new InvalidOperationException("该账号是租户所有者，不能直接删除；请先转移所有权。");
        }

        return memberships;
    }

    /// <summary>
    /// 仍然有效的所有者成员关系
    /// </summary>
    private static bool IsActiveOwner(SysTenantUser membership)
    {
        return membership.MemberType == TenantMemberType.Owner
               && membership.InviteStatus == TenantMemberInviteStatus.Accepted
               && membership.Status == ValidityStatus.Valid;
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

        var user = await GetUserOrThrowAsync(userId, cancellationToken);
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
    /// 授权是否占用角色成员名额：状态有效且未过期（尚未生效的预约同样占名额）
    /// </summary>
    private static bool OccupiesSeat(SysUserRole userRole, DateTimeOffset now)
    {
        return userRole.Status == ValidityStatus.Valid
               && (userRole.ExpirationTime is null || userRole.ExpirationTime > now);
    }

    /// <summary>
    /// 角色成员上限（MaxMembers，0 不限）：本次加入与移出之后不得超过上限
    /// </summary>
    private async Task EnsureRoleCapacityAsync(SysRole role, int joining, int leaving, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (role.MaxMembers <= 0 || joining <= leaving)
        {
            return;
        }

        var occupied = await _userRoleRepository.CountOccupiedByRoleIdAsync(role.BasicId, now, cancellationToken);
        if (occupied - leaving + joining > role.MaxMembers)
        {
            throw new InvalidOperationException($"角色「{role.RoleName}」最多 {role.MaxMembers} 个成员，当前已有 {occupied} 个。");
        }
    }

    /// <summary>
    /// 单条授权重新占用名额前的校验：与批量授予同一口径（成员上限 + 职责分离）
    /// </summary>
    private async Task EnsureRoleBindingCanJoinAsync(long userId, SysRole role, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await EnsureRoleCapacityAsync(role, joining: 1, leaving: 0, now, cancellationToken);

        var finalRoleIds = (await _userRoleRepository.GetValidByUserIdAsync(userId, now, cancellationToken))
            .Select(userRole => userRole.RoleId)
            .Append(role.BasicId)
            .Distinct();
        await EnsureNoSoDConflictAsync(finalRoleIds, cancellationToken);
    }

    /// <summary>
    /// 静态职责分离（SSD）执法：评估变更后最终有效角色集的约束违规。
    /// 拒绝/需审批类违规直接阻断授予；警告/记录日志类违规放行并留痕。
    /// </summary>
    private async Task EnsureNoSoDConflictAsync(IEnumerable<long> finalRoleIds, CancellationToken cancellationToken)
    {
        var result = await _constraintRuleEnforcementDomainService.EvaluateRoleAssignmentsAsync(
            finalRoleIds,
            ConstraintType.SSD,
            cancellationToken);

        var blocking = result.FirstBlockingViolation;
        if (blocking is not null)
        {
            throw new InvalidOperationException(
                $"角色授权违反职责分离约束规则[{blocking.RuleCode}]《{blocking.RuleName}》（冲突角色主键：{string.Join(",", blocking.MatchedTargetIds)}）。");
        }

        foreach (var violation in result.Violations)
        {
            _logger.LogWarning(
                "角色授权命中职责分离约束规则[{RuleCode}]《{RuleName}》，按 {ViolationAction} 处理放行。",
                violation.RuleCode,
                violation.RuleName,
                violation.ViolationAction);
        }
    }

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
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        EnsureAssignableRole(role);
        return role!;
    }

    /// <summary>
    /// 校验角色可分配给用户：存在、已启用，系统角色仅平台运维态可分配
    /// </summary>
    private void EnsureAssignableRole(SysRole? role)
    {
        if (role is null)
        {
            throw new InvalidOperationException("角色不存在。");
        }

        if (role.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("停用角色不能分配给用户。");
        }

        if (role.RoleType == RoleType.System && !_currentTenant.IsPlatformOperation())
        {
            throw new InvalidOperationException("系统角色仅平台运维态可分配，请切换到平台运维后操作。");
        }
    }

    /// <summary>
    /// 授权记录此刻是否生效：状态有效且落在生效 / 失效时间之间（与仓储 GetValidByUserIdAsync 同一口径）
    /// </summary>
    private static bool IsEffective(ValidityStatus status, DateTimeOffset? effectiveTime, DateTimeOffset? expirationTime, DateTimeOffset now)
    {
        return status == ValidityStatus.Valid
            && (effectiveTime is null || effectiveTime <= now)
            && (expirationTime is null || expirationTime > now);
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
    /// <param name="cancellationToken">取消令牌</param>
    /// <remarks>
    /// 支持成员（平台人员入驻）也由所在租户维护：平台看不到也写不了租户的授权数据，他们在租户里能做什么由这个租户决定。
    /// </remarks>
    private async Task<SysTenantUser> GetAssignableTenantMemberOrThrowAsync(
        long userId, DateTimeOffset now, string operationContext, CancellationToken cancellationToken)
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
