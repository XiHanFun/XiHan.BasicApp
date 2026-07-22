// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 用户站内信查询服务
/// </summary>
public interface IUserInboxQueryService
{
    /// <summary>
    /// 获取用户站内信列表
    /// </summary>
    Task<List<UserInboxItemDto>> GetListAsync(long userId, bool unreadOnly = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户「未读的强制阅读通知」（供路由守卫拦截）
    /// </summary>
    Task<List<UserInboxItemDto>> GetMandatoryUnreadAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户「当前生效的顶部横幅通知」
    /// </summary>
    Task<List<UserInboxItemDto>> GetBannerAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户「待弹出的登录后弹窗通知」（仅弹一次：PopupShownTime 为空）
    /// </summary>
    Task<List<UserInboxItemDto>> GetPopupAsync(long userId, CancellationToken cancellationToken = default);
}
