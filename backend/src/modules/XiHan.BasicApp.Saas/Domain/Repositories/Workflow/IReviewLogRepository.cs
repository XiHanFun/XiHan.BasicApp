// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 审查日志仓储接口
/// </summary>
public interface IReviewLogRepository : ISaasRepository<SysReviewLog>
{
    /// <summary>
    /// 添加审查日志
    /// </summary>
    Task AddLogAsync(SysReviewLog reviewLog, CancellationToken cancellationToken = default);
}
