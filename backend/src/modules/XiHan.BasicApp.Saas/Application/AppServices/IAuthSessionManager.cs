#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IAuthSessionManager
// Guid:01fcce60-df3d-4e1b-ac87-d8a1c550931a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 认证会话管理服务
/// </summary>
public interface IAuthSessionManager
{
    /// <summary>
    /// 按安全策略限制会话数量，返回被撤销的会话ID集合
    /// </summary>
    Task<IReadOnlyList<string>> EnforceSessionPolicyAsync(SysUser user, SysUserSecurity security, long? effectiveTenantId);

    /// <summary>
    /// 保存或更新会话
    /// </summary>
    Task SaveOrUpdateSessionAsync(SysUser user, long? effectiveTenantId, string sessionId, string accessTokenJti, ClientInfo clientInfo);

    /// <summary>
    /// 标记会话已撤销
    /// </summary>
    Task MarkSessionRevokedAsync(string sessionId, long? tenantId, string reason);

    /// <summary>
    /// 撤销用户所有会话
    /// </summary>
    Task RevokeUserSessionsAsync(long userId, string reason, long? tenantId);

    /// <summary>
    /// 检查会话是否有效（存在、未撤销、在线）
    /// </summary>
    Task<bool> IsSessionValidAsync(string sessionId, long? tenantId);
}
