// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 任务调度查询服务
/// </summary>
public interface ITaskSchedulerQueryService
{
    /// <summary>
    /// 获取启用的任务
    /// </summary>
    Task<IReadOnlyList<SysTask>> GetEnabledTasksAsync(CancellationToken cancellationToken = default);
}
