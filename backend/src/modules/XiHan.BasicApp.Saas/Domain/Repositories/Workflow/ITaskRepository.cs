// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 任务仓储接口
/// </summary>
public interface ITaskRepository : ISaasAggregateRepository<SysTask>
{
    /// <summary>
    /// 获取待执行任务列表
    /// </summary>
    Task<IReadOnlyList<SysTask>> GetPendingTasksAsync(CancellationToken cancellationToken = default);
}
