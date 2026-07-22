// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 第三方登录绑定仓储实现
/// </summary>
public sealed class ExternalLoginRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysExternalLogin>(clientResolver), IExternalLoginRepository
{
    /// <inheritdoc />
    public async Task<SysExternalLogin?> GetByProviderAndKeyAsync(string provider, string providerKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(login => login.Provider == provider && login.ProviderKey == providerKey)
            .FirstAsync(cancellationToken);
    }
}
