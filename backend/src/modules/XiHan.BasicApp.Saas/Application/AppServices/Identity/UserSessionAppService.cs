#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserSessionAppService
// Guid:8e9f0a1b-2c3d-4e4f-5a6b-7c8d9e0f1a2b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Events;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.EventBus.Abstractions.Local;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 用户会话命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户会话")]
public sealed class UserSessionAppService
    : SaasApplicationService, IUserSessionAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly ILocalEventBus _localEventBus;

    private readonly IUserDomainService _userDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserSessionAppService(
        IUserDomainService userDomainService,
        ICurrentUser currentUser,
        ILocalEventBus localEventBus)
    {
        _userDomainService = userDomainService;
        _currentUser = currentUser;
        _localEventBus = localEventBus;
    }

    #region 用户会话

    /// <summary>
    /// 撤销用户会话
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<UserSessionDetailDto> RevokeUserSessionAsync(UserSessionRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.RevokeUserSessionAsync(
            UserSessionApplicationMapper.ToRevokeCommand(input, _currentUser.UserId),
            cancellationToken);
        await PublishDomainEventAsync(result.DomainEvent, cancellationToken);
        return UserSessionApplicationMapper.ToDetailDto(result.Session, result.User, result.Now);
    }

    /// <summary>
    /// 撤销用户全部会话
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.UserSession.Revoke)]
    public async Task<int> RevokeUserSessionsAsync(UserSessionsRevokeDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _userDomainService.RevokeUserSessionsAsync(
            UserSessionApplicationMapper.ToRevokeAllCommand(input, _currentUser.UserId),
            cancellationToken);
        await PublishDomainEventAsync(result.DomainEvent, cancellationToken);
        return result.Count;
    }

    #endregion

    private async Task PublishDomainEventAsync(UserSessionRevokedDomainEvent? domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is null)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        await _localEventBus.PublishAsync(domainEvent);
    }

}
