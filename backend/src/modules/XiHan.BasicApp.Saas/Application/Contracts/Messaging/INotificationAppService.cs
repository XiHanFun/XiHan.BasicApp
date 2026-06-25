#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationAppService
// Guid:1fb6eb91-069a-4d56-a38b-dc737b709619
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

public interface INotificationAppService : IApplicationService
{
    Task<NotificationDetailDto> CreateNotificationAsync(NotificationCreateDto input, CancellationToken cancellationToken = default);

    Task<NotificationDetailDto> UpdateNotificationAsync(NotificationUpdateDto input, CancellationToken cancellationToken = default);

    Task<NotificationPublishResultDto> PublishNotificationAsync(NotificationPublishDto input, CancellationToken cancellationToken = default);

    Task DeleteNotificationAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 催办：对未读人员重新实时推送
    /// </summary>
    Task<NotificationPublishResultDto> RemindAsync(long id, CancellationToken cancellationToken = default);
}
