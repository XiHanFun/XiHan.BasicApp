#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:NotificationQueryService
// Guid:80912031-2435-4567-0123-456789abcd02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Caching.Attributes;
using XiHan.Framework.Core.DependencyInjection.ServiceLifetimes;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 通知查询服务
/// </summary>
public class NotificationQueryService : INotificationQueryService, ITransientDependency
{
    private readonly INotificationRepository _notificationRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public NotificationQueryService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <inheritdoc />
    [Cacheable(Key = "notif:id:{id}", ExpireSeconds = 300)]
    public async Task<NotificationDto?> GetByIdAsync(long id)
    {
        var entity = await _notificationRepository.GetByIdAsync(id);
        return entity?.Adapt<NotificationDto>();
    }
}
