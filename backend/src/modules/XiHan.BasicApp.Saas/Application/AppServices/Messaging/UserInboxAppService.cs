// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 确认通知
    /// </summary>
    [UnitOfWork(true)]
    public async Task ConfirmAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _userInboxDomainService.ConfirmAsync(input.BasicId, GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <summary>
    /// 获取当前用户站内信列表
    /// </summary>
    public async Task<List<UserInboxItemDto>> GetListAsync(bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userInboxQueryService.GetListAsync(GetCurrentUserIdOrThrow(), unreadOnly, cancellationToken);
    }

    /// <summary>
    /// 获取当前用户未读的强制阅读通知（路由守卫用）
    /// </summary>
    public async Task<List<UserInboxItemDto>> GetMandatoryUnreadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userInboxQueryService.GetMandatoryUnreadAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <summary>
    /// 获取当前用户当前生效的顶部横幅通知
    /// </summary>
    public async Task<List<UserInboxItemDto>> GetBannerAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userInboxQueryService.GetBannerAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <summary>
    /// 获取当前用户待弹出的登录后弹窗通知（仅弹一次）
    /// </summary>
    public async Task<List<UserInboxItemDto>> GetPopupAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _userInboxQueryService.GetPopupAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <summary>
    /// 标记登录后弹窗已展示
    /// </summary>
    [UnitOfWork(true)]
    public async Task MarkPopupShownAsync(UserInboxUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        await _userInboxDomainService.MarkPopupShownAsync(input.BasicId, GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <summary>
    /// 标记全部已读
    /// </summary>
    [UnitOfWork(true)]
    public async Task MarkAllReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _userInboxDomainService.MarkAllReadAsync(GetCurrentUserIdOrThrow(), cancellationToken);
    }

    /// <summary>
    /// 标记已读
    /// </summary>
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
