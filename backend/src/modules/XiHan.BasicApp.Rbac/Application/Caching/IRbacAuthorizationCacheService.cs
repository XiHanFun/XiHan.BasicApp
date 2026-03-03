#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IRbacAuthorizationCacheService
// Guid:cfe47f58-e7aa-4b13-b3a2-5fb357804b6b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/03 17:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Application.Caching;

/// <summary>
/// RBAC 授权缓存服务
/// </summary>
public interface IRbacAuthorizationCacheService
{
    /// <summary>
    /// 获取用户权限编码（带缓存）
    /// </summary>
    Task<IReadOnlyCollection<string>> GetUserPermissionCodesAsync(
        long userId,
        long? tenantId,
        Func<CancellationToken, Task<IReadOnlyCollection<string>>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户数据范围部门ID（带缓存）
    /// </summary>
    Task<IReadOnlyCollection<long>> GetUserDataScopeDepartmentIdsAsync(
        long userId,
        long? tenantId,
        Func<CancellationToken, Task<IReadOnlyCollection<long>>> factory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效权限缓存
    /// </summary>
    Task InvalidatePermissionAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效数据范围缓存
    /// </summary>
    Task InvalidateDataScopeAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效权限与数据范围缓存
    /// </summary>
    Task InvalidateAllAsync(long? tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取授权缓存版本快照
    /// </summary>
    Task<AuthorizationCacheVersionSnapshot> GetVersionSnapshotAsync(long? tenantId, CancellationToken cancellationToken = default);
}
