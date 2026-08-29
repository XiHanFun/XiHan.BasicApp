// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户安全命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户安全")]
public sealed class UserSecurityAppService
    : SaasApplicationService, IUserSecurityAppService
{
    private readonly IUserDomainService _userDomainService;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly IUserSessionRepository _userSessionRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSecurityAppService(
        IUserDomainService userDomainService,
        ISaasCacheInvalidator cacheInvalidator,
        ISuperAdminProtector superAdminProtector,
        IUserSessionRepository userSessionRepository)
    {
        _userDomainService = userDomainService;
        _cacheInvalidator = cacheInvalidator;
        _superAdminProtector = superAdminProtector;
        _userSessionRepository = userSessionRepository;
    }

    #region 用户安全

    /// <summary>
    /// 重置用户密码
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.ResetPassword)]
    public async Task<UserSecurityDetailDto> ResetUserPasswordAsync(UserPasswordResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得重置超管用户密码
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

        var result = await _userDomainService.ResetUserPasswordAsync(UserSecurityApplicationMapper.ToPasswordResetCommand(input), cancellationToken);
        // 重置密码后吊销该用户全部会话：安全戳只是写在库里的一个字段，没有任何地方读它，
        // 不吊销的话旧令牌一直用到会话自然过期，等于密码白改
        await RevokeAllSessionsAsync(input.UserId, cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 重置用户双因素认证（清除 OTP 绑定）
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.ResetTwoFactor)]
    public async Task<UserSecurityDetailDto> ResetUserTwoFactorAsync(UserTwoFactorResetDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得重置超管用户双因素认证
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

        var result = await _userDomainService.ResetUserTwoFactorAsync(UserSecurityApplicationMapper.ToTwoFactorResetCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 更新用户锁定状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.Lock)]
    public async Task<UserSecurityDetailDto> UpdateUserLockAsync(UserLockUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得锁定/解锁超管用户
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

        var result = await _userDomainService.UpdateUserLockAsync(UserSecurityApplicationMapper.ToLockCommand(input), cancellationToken);
        // 锁定后吊销该用户全部会话，否则锁定要等到会话自然过期才生效；解锁不涉及会话
        if (input.IsLocked)
        {
            await RevokeAllSessionsAsync(input.UserId, cancellationToken);
        }

        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 更新用户登录策略
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSecurity.LoginPolicy)]
    public async Task<UserSecurityDetailDto> UpdateUserLoginPolicyAsync(UserLoginPolicyUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得修改超管用户登录策略
        await _superAdminProtector.EnsureCanWriteUserAsync(input.UserId, cancellationToken);

        var result = await _userDomainService.UpdateUserLoginPolicyAsync(UserSecurityApplicationMapper.ToLoginPolicyCommand(input), cancellationToken);
        return UserSecurityApplicationMapper.ToDetailDto(result.Security, result.User, result.Now);
    }

    /// <summary>
    /// 吊销指定用户的全部会话并失效其会话状态缓存
    /// </summary>
    /// <remarks>
    /// 模仿会话行的 UserId 是被模仿者，由该用户发起的模仿会话须按模仿者另吊销一次。
    /// 仓储级批量吊销不走领域事件，会话闸门读的是 60 秒 TTL 的缓存，须自行失效。
    /// </remarks>
    /// <param name="userId">用户主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task RevokeAllSessionsAsync(long userId, CancellationToken cancellationToken)
    {
        var revokedSessionIds = await _userSessionRepository.RevokeByUserIdAsync(userId, cancellationToken);
        var revokedImpersonationIds = await _userSessionRepository.RevokeByImpersonatorUserIdAsync(userId, cancellationToken);
        foreach (var userSessionId in revokedSessionIds.Concat(revokedImpersonationIds))
        {
            await _cacheInvalidator.InvalidateSessionStateAsync(userSessionId, cancellationToken);
        }
    }

    #endregion
}
