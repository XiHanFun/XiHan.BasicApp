#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IExportTaskAppService
// Guid:c2b9d7f5-0a3e-4f8b-94c6-6e5f70819203
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 导出任务应用服务接口（写侧：提交 / 取消 / 删除，均为当前用户自有任务）
/// </summary>
public interface IExportTaskAppService : IApplicationService
{
    /// <summary>
    /// 提交导出任务（落 Pending，由后台 worker 异步执行）
    /// </summary>
    Task<ExportTaskDto> SubmitAsync(ExportTaskSubmitDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消待执行任务（仅 Pending 可取消，自鉴权）
    /// </summary>
    Task CancelAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除任务记录（自鉴权软删）
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
