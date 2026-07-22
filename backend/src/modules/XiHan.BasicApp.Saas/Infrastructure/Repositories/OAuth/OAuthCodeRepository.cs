// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    public async Task<SysOAuthCode?> GetByCodeIgnoreTenantAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(c => c.Code == code)
            .FirstAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> TryConsumeAsync(long codeId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 条件更新在数据库层保证原子性，不受租户查询过滤影响（按主键 + 未使用双条件命中唯一行）：
        // 仅当 Is_Used=false 时翻转为 true，受影响行数=1 表示本次调用赢得竞态，可继续换取令牌。
        var affected = await DbClient.Updateable<SysOAuthCode>()
            .SetColumns(c => new SysOAuthCode { IsUsed = true, UsedTime = now })
            .Where(c => c.BasicId == codeId && !c.IsUsed)
            .ExecuteCommandAsync(cancellationToken);
        return affected == 1;
    }

    /// <inheritdoc />
    public async Task<int> CleanExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await DbClient.Deleteable<SysOAuthCode>()
            .Where(code => code.ExpirationTime < now)
            .ExecuteCommandAsync(cancellationToken);
    }
}
