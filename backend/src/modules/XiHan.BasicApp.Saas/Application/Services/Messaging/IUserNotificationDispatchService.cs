#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserNotificationDispatchService
// Guid:50d9019a-7613-48d2-af29-e329e06b91bb
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/04 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
