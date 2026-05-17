#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ReviewLogRepository
// Guid:32b3a677-7150-4282-a60b-95425d7f902d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
