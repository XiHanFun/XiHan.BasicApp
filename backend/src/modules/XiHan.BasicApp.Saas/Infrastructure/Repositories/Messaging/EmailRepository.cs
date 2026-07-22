// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 邮件仓储实现
/// </summary>
public sealed class EmailRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysEmail>(clientResolver), IEmailRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SysEmail>> GetPendingEmailsAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateQueryable()
            .Where(email => email.EmailStatus == EmailStatus.Pending)
            .OrderBy(email => email.CreatedTime)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }
}
