#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExportTaskHostedService
// Guid:46d3a1c9-4f7b-4c2e-3b05-b7c8d9e0f102
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Application.Exporting;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Infrastructure.Exporting;

/// <summary>
/// 导出任务后台执行服务。
/// </summary>
/// <remarks>
/// 框架无即时任务队列，采用「常驻轮询 SysExportTask 的 Pending 任务」承载异步导出：
/// 重启后未完成（残留 Processing）任务会被复位为 Pending 重新执行，契合「关浏览器第二天再下载」。
/// P0 单实例串行处理（每任务独立 DI scope）；并发可后续扩展。
/// 领取在无租户上下文下进行（跨租户扫描），具体执行由 ExportExecutor 重建发起人上下文后完成。
/// </remarks>
public sealed class ExportTaskHostedService : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(3);

    private readonly ILogger<ExportTaskHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExportTaskHostedService(IServiceScopeFactory scopeFactory, ILogger<ExportTaskHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ResetOrphanedAsync(stoppingToken);

        _logger.LogInformation("导出任务后台服务已启动");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextAsync(stoppingToken);
                if (!processed)
                {
                    await Task.Delay(IdleDelay, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出任务后台轮询异常");
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }

        _logger.LogInformation("导出任务后台服务已停止");
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IExportTaskRepository>();

        var task = await repository.ClaimNextPendingAsync(DateTimeOffset.UtcNow, cancellationToken);
        if (task is null)
        {
            return false;
        }

        var executor = scope.ServiceProvider.GetRequiredService<IExportExecutor>();
        await executor.ExecuteAsync(task, cancellationToken);
        return true;
    }

    private async Task ResetOrphanedAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IExportTaskRepository>();
            var count = await repository.ResetOrphanedProcessingAsync(cancellationToken);
            if (count > 0)
            {
                _logger.LogInformation("已复位崩溃残留的执行中导出任务");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "复位残留导出任务失败（忽略，继续启动）");
        }
    }
}
