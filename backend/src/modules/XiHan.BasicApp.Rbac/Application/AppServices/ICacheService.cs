#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICacheService
// Guid:feeb119e-8c9a-48ec-aac6-7fdcf6d63f84
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 14:13:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.AppServices;

/// <summary>
/// 系统缓存服务
/// </summary>
public interface ICacheService : IApplicationService
{
    /// <summary>
    /// 获取缓存字符串
    /// </summary>
    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置缓存字符串
    /// </summary>
    Task SetStringAsync(string key, string value, int expireSeconds = 300, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除缓存项
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除缓存项
    /// </summary>
    Task RemoveManyAsync(IReadOnlyCollection<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效授权缓存
    /// </summary>
    Task InvalidateAuthorizationAsync(long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效查找缓存
    /// </summary>
    Task InvalidateLookupAsync(long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效消息缓存
    /// </summary>
    Task InvalidateMessageAsync(long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效全部缓存
    /// </summary>
    Task InvalidateAllAsync(long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取缓存版本快照
    /// </summary>
    Task<SysCacheSnapshotDto> GetSnapshotAsync(long? tenantId = null, CancellationToken cancellationToken = default);
}
