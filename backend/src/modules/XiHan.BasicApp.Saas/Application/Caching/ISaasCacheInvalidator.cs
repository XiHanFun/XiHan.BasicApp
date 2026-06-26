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

    /// <summary>
    /// 失效已启用租户版本列表缓存。
    /// </summary>
    Task InvalidateTenantEditionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效资源定义（可选资源选择项）缓存。
    /// </summary>
    Task InvalidateResourceDefinitionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效操作定义（可选操作选择项）缓存。
    /// </summary>
    Task InvalidateOperationDefinitionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效组织结构（部门树）缓存。
    /// </summary>
    Task InvalidateOrganizationAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效指定用户的设置缓存（写后整体失效该用户全部场景）。
    /// </summary>
    Task InvalidateUserSettingAsync(long userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效消息模板缓存（模板增删改/启停后调用，发送链路按 渠道+编码 高频读取）。
    /// </summary>
    Task InvalidateMessageTemplateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效版本门控缓存（版本权限白名单变更/租户换版本后调用，鉴权快照热路径）。
    /// </summary>
    Task InvalidateEditionGateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效字典项树缓存（字典/字典项增删改/启停后调用）。
    /// </summary>
    Task InvalidateDictionaryAsync(CancellationToken cancellationToken = default);
}
