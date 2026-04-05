#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:INotificationQueryService
// Guid:80912031-2435-4567-0123-456789abcd01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 通知查询服务接口
/// </summary>
public interface INotificationQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取通知
    /// </summary>
    Task<NotificationDto?> GetByIdAsync(long id);
}
