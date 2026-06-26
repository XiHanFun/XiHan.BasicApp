#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExportExecutor
// Guid:24b1e9a7-2d5f-4a0c-19e3-9506b7c8d9e0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
