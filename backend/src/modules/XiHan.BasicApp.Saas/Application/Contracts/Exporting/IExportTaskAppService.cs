// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
