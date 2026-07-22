// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 导出执行器：在已领取（Processing）的任务上重建上下文、拉数、写出、落产物、回写状态。
/// </summary>
public interface IExportExecutor
{
    /// <summary>
    /// 执行一个已领取的导出任务
    /// </summary>
    Task ExecuteAsync(SysExportTask task, CancellationToken cancellationToken = default);
}
