// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 当前用户通知投递服务接口
/// </summary>
public interface IUserNotificationDispatchService
{
    /// <summary>
    /// 投递用户通知并尝试实时推送
    /// </summary>
    Task<UserInboxItemDto> DispatchToUserAsync(
        long userId,
        string title,
        string? content,
        NotificationType notificationType = NotificationType.System,
        string? businessType = null,
        long? businessId = null,
        bool needConfirm = false,
        string? link = null,
        string? icon = null,
        CancellationToken cancellationToken = default);
}
