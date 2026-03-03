#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRbacLookupCacheService
// Guid:4a0d83fb-cf8d-4b89-b9ad-a2e5fd4f6e64
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/04 13:38:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;

namespace XiHan.BasicApp.Rbac.Application.Caching;

/// <summary>
/// RBAC 查找缓存服务
/// </summary>
public interface IRbacLookupCacheService
{
    /// <summary>
    /// 获取文件（按哈希）
    /// </summary>
    Task<FileDto?> GetFileByHashAsync(
        string fileHash,
        long? tenantId,
        Func<CancellationToken, Task<FileDto?>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取任务（按编码）
    /// </summary>
    Task<TaskDto?> GetTaskByCodeAsync(
        string taskCode,
        long? tenantId,
        Func<CancellationToken, Task<TaskDto?>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取 OAuth 应用（按客户端ID）
    /// </summary>
    Task<OAuthAppDto?> GetOAuthAppByClientIdAsync(
        string clientId,
        long? tenantId,
        Func<CancellationToken, Task<OAuthAppDto?>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效文件查找缓存
    /// </summary>
    Task InvalidateFileLookupAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效任务查找缓存
    /// </summary>
    Task InvalidateTaskLookupAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效 OAuth 应用查找缓存
    /// </summary>
    Task InvalidateOAuthAppLookupAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效全部查找缓存
    /// </summary>
    Task InvalidateAllAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取版本快照
    /// </summary>
    Task<LookupCacheVersionSnapshot> GetVersionSnapshotAsync(long? tenantId, CancellationToken cancellationToken = default);
}
