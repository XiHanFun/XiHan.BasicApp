// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// OAuth 应用仓储实现
/// </summary>
public sealed class OAuthAppRepository(
    ISqlSugarClientResolver clientResolver,
    IUnitOfWorkManager unitOfWorkManager)
    : SaasAggregateRepository<SysOAuthApp>(clientResolver, unitOfWorkManager), IOAuthAppRepository
{
    /// <summary>
    /// 根据客户端ID获取
    /// </summary>
    public async Task<SysOAuthApp?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(app => app.ClientId == clientId)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 根据客户端ID跨租户获取（ClientId 全局唯一；供匿名 /connect/token 等无租户上下文场景使用）
    /// </summary>
    public async Task<SysOAuthApp?> GetByClientIdIgnoreTenantAsync(string clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(app => app.ClientId == clientId)
            .FirstAsync(cancellationToken);
    }
}
