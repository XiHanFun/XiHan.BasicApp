#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationQueryService
// Guid:7c1d2978-e9b0-44bc-92cc-accd2247a6a7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 系统通知查询应用服务接口
/// </summary>
public interface INotificationQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统通知分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统通知分页列表</returns>
    Task<PageResultDtoBase<NotificationListItemDto>> GetNotificationPageAsync(NotificationPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统通知详情
    /// </summary>
    /// <param name="id">系统通知主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统通知详情</returns>
    Task<NotificationDetailDto?> GetNotificationDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户通知分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户通知分页列表</returns>
    Task<PageResultDtoBase<UserNotificationListItemDto>> GetUserNotificationPageAsync(UserNotificationPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户通知详情
    /// </summary>
    /// <param name="id">用户通知主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户通知详情</returns>
    Task<UserNotificationDetailDto?> GetUserNotificationDetailAsync(long id, CancellationToken cancellationToken = default);
}
