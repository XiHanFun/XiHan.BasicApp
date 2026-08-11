// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 失效指定会话的状态缓存（锁定/解锁/吊销/登出后必须调用）。
    /// </summary>
    /// <remarks>
    /// 会话闸门每请求读这份缓存。任何改写 <c>SysUserSession.Status</c> 或 <c>IsLocked</c> 的写路径
    /// <b>都必须</b>补这一刀，否则改动最长要等缓存过期才生效——踢下线踢不掉、锁定锁不住。
    /// </remarks>
    /// <param name="userSessionId">会话业务标识（JWT 的 session_id）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task InvalidateSessionStateAsync(string userSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效全部会话状态缓存（批量吊销某用户全部会话时调用）。
    /// </summary>
    Task InvalidateAllSessionStatesAsync(CancellationToken cancellationToken = default);
}
