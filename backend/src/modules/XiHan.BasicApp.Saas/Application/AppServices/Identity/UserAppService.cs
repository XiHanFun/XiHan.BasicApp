// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Caching;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户")]
public sealed class UserAppService
    : SaasApplicationService, IUserAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly IUserDomainService _userDomainService;

    private readonly IUserRepository _userRepository;

    private readonly IFieldSecurityService _fieldSecurity;

    private readonly ISuperAdminProtector _superAdminProtector;

    private readonly ISaasCacheInvalidator _cacheInvalidator;

    private readonly IUserSessionRepository _userSessionRepository;

    private readonly IRealtimeNotificationService<BasicAppNotificationHub> _realtimeNotificationService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserAppService(
        IUserDomainService userDomainService,
        IUserRepository userRepository,
        IFieldSecurityService fieldSecurity,
        ISuperAdminProtector superAdminProtector,
        ICurrentUser currentUser,
        ISaasCacheInvalidator cacheInvalidator,
        IUserSessionRepository userSessionRepository,
        IRealtimeNotificationService<BasicAppNotificationHub> realtimeNotificationService)
    {
        _userDomainService = userDomainService;
        _userRepository = userRepository;
        _fieldSecurity = fieldSecurity;
        _superAdminProtector = superAdminProtector;
        _currentUser = currentUser;
        _cacheInvalidator = cacheInvalidator;
        _userSessionRepository = userSessionRepository;
        _realtimeNotificationService = realtimeNotificationService;
    }

    #region 用户核心

    /// <summary>
    /// 创建用户
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Create)]
    public async Task<UserDetailDto> CreateUserAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.CreateUserAsync(
            UserApplicationMapper.ToCreateCommand(input, _currentUser.UserId),
            cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Delete)]
    public async Task DeleteUserAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // 超管保护：非超管不得删除超管用户
        await _superAdminProtector.EnsureCanWriteUserAsync(id, cancellationToken);
        await _userDomainService.DeleteUserAsync(id, cancellationToken);
        // 删除用户后：吊销其全部会话（请求期会话校验随即拒绝）+ 失效授权快照
        await _userSessionRepository.RevokeByUserIdAsync(id, cancellationToken);
        await _cacheInvalidator.InvalidateAuthorizationAsync(id, cancellationToken);

        // 实时踢出被删用户的在线连接（仓储级批量吊销不走领域事件，这里直推 ForceLogout）
        try
        {
            await _realtimeNotificationService.SendToUserAsync(
                id.ToString(),
                SignalRConstants.ClientMethods.ForceLogout,
                new { reason = "账号已被删除，如有疑问请联系管理员。", targetSessionIds = (string[]?)null });
        }
        catch
        {
            // 实时推送失败不影响删除主流程（令牌校验仍会拒绝已吊销会话）
        }
    }

    /// <summary>
    /// 更新用户资料
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Update)]
    public async Task<UserDetailDto> UpdateUserAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得修改超管用户
        await _superAdminProtector.EnsureCanWriteUserAsync(input.BasicId, cancellationToken);

        // 写校验：FLS 不可编辑字段不得被修改（防绕过前端只读直接调 API）
        var current = await _userRepository.GetByIdAsync(input.BasicId, cancellationToken);
        if (current is not null)
        {
            await _fieldSecurity.EnsureUpdatableAsync("SysUser", input, current, cancellationToken);
        }

        var result = await _userDomainService.UpdateUserAsync(UserApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    /// <summary>
    /// 更新用户状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.User.Status)]
    public async Task<UserDetailDto> UpdateUserStatusAsync(UserStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        // 超管保护：非超管不得启停超管用户
        await _superAdminProtector.EnsureCanWriteUserAsync(input.BasicId, cancellationToken);

        var result = await _userDomainService.UpdateUserStatusAsync(UserApplicationMapper.ToStatusCommand(input), cancellationToken);
        // 禁用时吊销其全部会话，使禁用即时生效（请求期会话校验随即拒绝；禁用用户重新启用后需重新登录）
        if (input.Status == EnableStatus.Disabled)
        {
            await _userSessionRepository.RevokeByUserIdAsync(input.BasicId, cancellationToken);
        }

        await _cacheInvalidator.InvalidateAuthorizationAsync(input.BasicId, cancellationToken);
        return UserApplicationMapper.ToDetailDto(result.User);
    }

    #endregion
}
