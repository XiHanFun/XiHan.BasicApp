// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 短信仓储实现
/// </summary>
public sealed class SmsRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysSms>(clientResolver), ISmsRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysSms>> GetPendingSmsAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(sms => sms.SmsStatus == SmsStatus.Pending)
            .OrderBy(sms => sms.CreatedTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }
}
