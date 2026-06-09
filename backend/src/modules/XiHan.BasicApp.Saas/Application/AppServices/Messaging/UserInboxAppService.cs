#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserInboxAppService
// Guid:c7e61e82-8a58-45bb-ae47-25f24f3eea06
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 当前用户站内信应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "用户站内信")]
public sealed class UserInboxAppService
    : SaasApplicationService, IUserInboxAppService
{
    private readonly ICurrentUser _currentUser;
    private readonly IUserInboxDomainService _userInboxDomainService;
    private readonly IUserInboxQueryService _userInboxQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserInboxAppService(
        IUserInboxDomainService userInboxDomainService,
        IUserInboxQueryService userInboxQueryService,
        ICurrentUser currentUser)
    {
        _userInboxDomainService = userInboxDomainService;
        _userInboxQueryService = userInboxQueryService;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task ConfirmAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _userInboxDomainService.ConfirmAsync(input.BasicId, GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<UserInboxItemDto>> GetListAsync(bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userInboxQueryService.GetListAsync(GetCurrentUserIdOrThrow(), unreadOnly, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userInboxDomainService.MarkAllReadAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    public async Task MarkReadAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _userInboxDomainService.MarkReadAsync(input.BasicId, GetCurrentUserIdOrThrow(), cancellationToken);
    }

    private long GetCurrentUserIdOrThrow()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }
}
