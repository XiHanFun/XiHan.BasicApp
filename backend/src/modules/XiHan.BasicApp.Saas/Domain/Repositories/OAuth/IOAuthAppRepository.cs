// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// OAuth 应用仓储接口
/// </summary>
public interface IOAuthAppRepository : ISaasAggregateRepository<SysOAuthApp>
{
    /// <summary>
    /// 根据客户端ID获取
    /// </summary>
    Task<SysOAuthApp?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据客户端ID跨租户获取（ClientId 全局唯一；供匿名 /connect/token 等无租户上下文场景使用）
    /// </summary>
    Task<SysOAuthApp?> GetByClientIdIgnoreTenantAsync(string clientId, CancellationToken cancellationToken = default);
}
