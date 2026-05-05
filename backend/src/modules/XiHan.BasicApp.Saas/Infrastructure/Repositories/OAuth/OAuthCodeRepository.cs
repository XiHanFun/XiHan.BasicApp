#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthCodeRepository
// Guid:723c850f-f808-492a-990d-9e24b2ddf1e1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// OAuth 授权码仓储实现
/// </summary>
public sealed class OAuthCodeRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysOAuthCode>(clientResolver), IOAuthCodeRepository
{
    /// <inheritdoc />
    public async Task<SysOAuthCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(c => c.Code == code)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> CleanExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbClient.Deleteable<SysOAuthCode>()
            .Where(code => code.ExpiresTime < now)
            .ExecuteCommandAsync(cancellationToken);
    }
}
