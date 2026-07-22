// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 审查日志仓储实现
/// </summary>
public sealed class ReviewLogRepository
    : SaasRepository<SysReviewLog>, IReviewLogRepository
{
    /// <summary>
    /// 构造函数
    /// </summary>
    public ReviewLogRepository(ISqlSugarClientResolver clientResolver)
        : base(clientResolver)
    {
    }

    /// <inheritdoc />
    public async Task AddLogAsync(SysReviewLog reviewLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reviewLog);
        cancellationToken.ThrowIfCancellationRequested();

        await DbClient.Insertable(reviewLog).SplitTable().ExecuteCommandAsync();
    }
}
