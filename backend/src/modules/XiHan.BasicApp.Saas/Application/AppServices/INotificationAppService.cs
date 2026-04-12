#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationAppService
// Guid:d67eafab-f2f0-4fdf-b31f-f7cd0a6a7fd2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 10:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.UseCases.Commands;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 通知管理服务（管理页 CRUD + 发布 + 内部推送）
/// </summary>
public interface INotificationAppService
    : ICrudApplicationService<NotificationDto, long, NotificationCreateDto, NotificationUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 推送通知（内部调用，创建并直接发布）
    /// </summary>
    Task<int> PushAsync(PushNotificationCommand command);

    /// <summary>
    /// 发布已有草稿通知
    /// </summary>
    Task<int> PublishAsync(long notificationId);

    /// <summary>
    /// 获取通知的接收用户ID列表（管理页编辑草稿时回填接收人）
    /// </summary>
    Task<IReadOnlyList<long>> GetRecipientsAsync(long notificationId);
}
