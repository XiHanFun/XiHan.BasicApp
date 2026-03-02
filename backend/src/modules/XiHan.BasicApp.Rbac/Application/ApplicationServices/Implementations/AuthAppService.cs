#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthAppService
// Guid:f3f91e56-9d5b-4f30-8f75-f6f84b714442
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 15:35:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Commands;
using XiHan.BasicApp.Rbac.Application.Caching;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Application.Queries;
using XiHan.BasicApp.Rbac.Domain.DomainServices;
using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.BasicApp.Rbac.Domain.Enums;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.BasicApp.Rbac.Domain.ValueObjects;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Authentication.Password;
using XiHan.Framework.Uow;
using XiHan.Framework.Uow.Options;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 认证应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Rbac", GroupName = "系统Rbac服务")]
public class AuthAppService : ApplicationServiceBase, IAuthAppService
{
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;

    private readonly IUserRepository _userRepository;
    private readonly IUserManager _userManager;
    private readonly IAuthorizationDomainService _authorizationDomainService;
    private readonly IRbacAuthorizationCacheService _authorizationCacheService;
    private readonly ILoginLogRepository _loginLogRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository"></param>
    /// <param name="userManager"></param>
    /// <param name="authorizationDomainService"></param>
    /// <param name="authorizationCacheService"></param>
    /// <param name="loginLogRepository"></param>
    /// <param name="passwordHasher"></param>
    /// <param name="unitOfWorkManager"></param>
    public AuthAppService(
        IUserRepository userRepository,
        IUserManager userManager,
        IAuthorizationDomainService authorizationDomainService,
        IRbacAuthorizationCacheService authorizationCacheService,
        ILoginLogRepository loginLogRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _authorizationDomainService = authorizationDomainService;
        _authorizationCacheService = authorizationCacheService;
        _loginLogRepository = loginLogRepository;
        _passwordHasher = passwordHasher;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task<UserLoginResultDto> LoginAsync(UserLoginCommand command)
    {
        command.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByUserNameAsync(command.UserName.Trim(), command.TenantId);
        if (user is null)
        {
            await WriteLoginLogAsync(0, command, LoginResult.InvalidCredentials, "用户名或密码错误");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                LoginResult = LoginResult.InvalidCredentials,
                Message = "用户名或密码错误"
            };
        }

        if (user.Status != YesOrNo.Yes)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountDisabled, "账号已禁用");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                UserId = user.BasicId,
                UserName = user.UserName,
                LoginResult = LoginResult.AccountDisabled,
                Message = "账号已禁用"
            };
        }

        var security = await EnsureSecurityProfileAsync(user);
        if (security.IsLocked && security.LockoutEndTime.HasValue && security.LockoutEndTime > DateTimeOffset.UtcNow)
        {
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.AccountLocked, "账号已锁定");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                UserId = user.BasicId,
                UserName = user.UserName,
                LoginResult = LoginResult.AccountLocked,
                Message = $"账号已锁定，请 {security.LockoutEndTime:HH:mm} 后重试"
            };
        }

        var password = PasswordValueObject.FromHash(user.Password);
        if (!password.Verify(command.Password, _passwordHasher))
        {
            await HandlePasswordFailureAsync(security);
            await WriteLoginLogAsync(user.BasicId, command, LoginResult.InvalidCredentials, "用户名或密码错误");
            await uow.CompleteAsync();
            return new UserLoginResultDto
            {
                Success = false,
                UserId = user.BasicId,
                UserName = user.UserName,
                LoginResult = LoginResult.InvalidCredentials,
                Message = "用户名或密码错误"
            };
        }

        security.FailedLoginAttempts = 0;
        security.IsLocked = false;
        security.LockoutTime = null;
        security.LockoutEndTime = null;
        security.LastFailedLoginTime = null;
        security.LastSecurityCheckTime = DateTimeOffset.UtcNow;
        await _userRepository.SaveSecurityAsync(security);

        user.LastLoginTime = DateTimeOffset.UtcNow;
        await _userRepository.UpdateAsync(user);

        var permissionCodes = await _authorizationCacheService.GetUserPermissionCodesAsync(
            user.BasicId,
            user.TenantId,
            token => _authorizationDomainService.GetUserPermissionCodesAsync(user.BasicId, user.TenantId, token));
        await WriteLoginLogAsync(user.BasicId, command, LoginResult.Success, "登录成功");
        await uow.CompleteAsync();

        return new UserLoginResultDto
        {
            Success = true,
            UserId = user.BasicId,
            UserName = user.UserName,
            LoginResult = LoginResult.Success,
            Message = "登录成功",
            PermissionCodes = permissionCodes
        };
    }

    /// <summary>
    /// 修改密码
    /// </summary>
    /// <param name="command"></param>
    /// <returns></returns>
    public async Task ChangePasswordAsync(ChangePasswordCommand command)
    {
        command.ValidateAnnotations();

        using var uow = _unitOfWorkManager.Begin(new XiHanUnitOfWorkOptions(), true);
        var user = await _userRepository.GetByIdAsync(command.UserId)
                   ?? throw new KeyNotFoundException($"未找到用户: {command.UserId}");

        var currentPassword = PasswordValueObject.FromHash(user.Password);
        if (!currentPassword.Verify(command.OldPassword, _passwordHasher))
        {
            throw new InvalidOperationException("原密码错误");
        }

        await _userManager.ChangePasswordAsync(user, command.NewPassword);
        await uow.CompleteAsync();
    }

    /// <summary>
    /// 获取用户权限编码
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<string>> GetPermissionCodesAsync(UserPermissionQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _authorizationCacheService.GetUserPermissionCodesAsync(
            query.UserId,
            query.TenantId,
            token => _authorizationDomainService.GetUserPermissionCodesAsync(query.UserId, query.TenantId, token));
    }

    /// <summary>
    /// 获取用户数据范围部门ID
    /// </summary>
    /// <param name="query"></param>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<long>> GetDataScopeDepartmentIdsAsync(UserDataScopeQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await _authorizationCacheService.GetUserDataScopeDepartmentIdsAsync(
            query.UserId,
            query.TenantId,
            token => _authorizationDomainService.GetUserDataScopeDepartmentIdsAsync(query.UserId, query.TenantId, token));
    }

    /// <summary>
    /// 写入登录日志
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="command"></param>
    /// <param name="loginResult"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task WriteLoginLogAsync(long userId, UserLoginCommand command, LoginResult loginResult, string message)
    {
        var log = new SysLoginLog
        {
            TenantId = command.TenantId,
            UserId = userId,
            UserName = command.UserName,
            LoginResult = loginResult,
            Message = message,
            LoginTime = DateTimeOffset.UtcNow
        };

        await _loginLogRepository.AddAsync(log);
    }

    /// <summary>
    /// 确保安全配置文件
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    private async Task<SysUserSecurity> EnsureSecurityProfileAsync(SysUser user)
    {
        var security = await _userRepository.GetSecurityByUserIdAsync(user.BasicId, user.TenantId);
        if (security is not null)
        {
            return security;
        }

        security = new SysUserSecurity
        {
            TenantId = user.TenantId,
            UserId = user.BasicId,
            FailedLoginAttempts = 0,
            IsLocked = false,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        return await _userRepository.SaveSecurityAsync(security);
    }

    /// <summary>
    /// 处理密码失败
    /// </summary>
    /// <param name="security"></param>
    /// <returns></returns>
    private async Task HandlePasswordFailureAsync(SysUserSecurity security)
    {
        security.FailedLoginAttempts += 1;
        security.LastFailedLoginTime = DateTimeOffset.UtcNow;

        if (security.FailedLoginAttempts >= MaxFailedAttempts)
        {
            security.IsLocked = true;
            security.LockoutTime = DateTimeOffset.UtcNow;
            security.LockoutEndTime = DateTimeOffset.UtcNow.AddMinutes(LockoutMinutes);
        }

        await _userRepository.SaveSecurityAsync(security);
    }
}
