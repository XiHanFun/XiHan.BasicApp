#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISaasCacheInvalidator
// Guid:4bec3f98-8d0b-4104-90c0-73199c7a0b3b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Caching;

/// <summary>
/// SaaS 模块缓存失效器。
/// </summary>
public interface ISaasCacheInvalidator
{
    /// <summary>
    /// 失效配置缓存。
    /// </summary>
    Task InvalidateConfigurationAsync(string? configKey = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效授权快照缓存。
    /// </summary>
    Task InvalidateAuthorizationAsync(long? userId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效菜单路由缓存。
    /// </summary>
    Task InvalidateNavigationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效权限定义（可选权限选择项）缓存。
    /// </summary>
    Task InvalidatePermissionDefinitionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效角色定义（已启用角色选择项）缓存。
    /// </summary>
    Task InvalidateRoleDefinitionAsync(CancellationToken cancellationToken = default);
}
