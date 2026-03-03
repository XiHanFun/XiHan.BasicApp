#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IMessageCacheService
// Guid:fae20f25-5f95-40b7-a824-df17c8f70366
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:48:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Caching;

/// <summary>
/// 消息缓存服务
/// </summary>
public interface IMessageCacheService
{
    /// <summary>
    /// 获取用户未读数量
    /// </summary>
    Task<int> GetUnreadCountAsync(
        long userId,
        long? tenantId,
        Func<CancellationToken, Task<int>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效未读缓存
    /// </summary>
    Task InvalidateUnreadCountAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未读缓存版本
    /// </summary>
    Task<long> GetUnreadVersionAsync(long? tenantId, CancellationToken cancellationToken = default);
}
