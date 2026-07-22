// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Claims;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 导出执行器实现。
/// </summary>
/// <remarks>
/// 安全要点：后台线程无 HTTP 上下文，先用任务发起人（CreatedId/TenantId）重建 CurrentTenant + CurrentPrincipal，
/// 再调既有 QueryService —— 数据范围/字段脱敏原样生效；并显式 IPermissionChecker 校验，补 [PermissionAuthorize]
/// 进程内不触发的缺口。整体规避「越权」与「字段语义在前端」两条暂缓理由。
/// </remarks>
public sealed class ExportExecutor : IExportExecutor
{
    private readonly ICurrentPrincipalAccessor _principalAccessor;
    private readonly ICurrentTenant _currentTenant;
    private readonly IFileTransferService _fileTransferService;
    private readonly ILogger<ExportExecutor> _logger;
    private readonly IUserTaskProgressNotifier _notifier;
    private readonly IPermissionChecker _permissionChecker;
    private readonly IReadOnlyDictionary<string, IExportProvider> _providers;
    private readonly IExportTaskRepository _repository;
    private readonly IReadOnlyList<IExportWriter> _writers;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExportExecutor(
        IEnumerable<IExportProvider> providers,
        IEnumerable<IExportWriter> writers,
        IExportTaskRepository repository,
        IFileTransferService fileTransferService,
        ICurrentTenant currentTenant,
        ICurrentPrincipalAccessor principalAccessor,
        IPermissionChecker permissionChecker,
        IUserTaskProgressNotifier notifier,
        ILogger<ExportExecutor> logger)
    {
        _providers = providers
            .GroupBy(provider => provider.BusinessType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        _writers = [.. writers];
        _repository = repository;
        _fileTransferService = fileTransferService;
        _currentTenant = currentTenant;
        _principalAccessor = principalAccessor;
        _permissionChecker = permissionChecker;
        _notifier = notifier;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(SysExportTask task, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(task);

        var userId = task.CreatedId;
        var tenantId = task.TenantId;
        var islandTaskId = $"export:{task.BasicId}";

        // 重建发起人上下文：租户 + 主体（claims 含 UserId/TenantId），使下游 QueryService 的
        // 数据范围/字段脱敏按发起人生效
        using var tenantScope = _currentTenant.Change(tenantId);
        using var principalScope = _principalAccessor.Change(BuildPrincipal(userId, tenantId));

        try
        {
            await _notifier.NotifyRunningAsync(userId, islandTaskId, $"正在导出 {task.TaskName}", "准备中…", 0, cancellationToken);

            if (!_providers.TryGetValue(task.BusinessType, out var provider))
            {
                await FailAsync(task, userId, islandTaskId, $"该资源未接入导出（{task.BusinessType}）", cancellationToken);
                return;
            }

            // 进程内权限校验（[PermissionAuthorize] 仅在 HTTP 管道触发，直调不触发）
            var granted = await _permissionChecker.IsGrantedAsync(userId.ToString(), provider.RequiredPermission, cancellationToken);
            if (!granted)
            {
                await FailAsync(task, userId, islandTaskId, "无导出权限", cancellationToken);
                return;
            }

            var columns = DeserializeColumns(task.FieldsSnapshot);
            if (columns.Count == 0)
            {
                await FailAsync(task, userId, islandTaskId, "导出列为空", cancellationToken);
                return;
            }

            var writer = _writers.FirstOrDefault(item => item.Format == task.Format)
                ?? _writers.First(item => item.Format == ExportFormat.Csv);

            // 实际落地的格式以命中的写出器为准（请求格式无匹配时回落 CSV），文件名/ContentType 都跟着它走
            var actualFormat = writer.Format;

            var context = new ExportContext
            {
                BusinessType = task.BusinessType,
                Scope = task.Scope,
                QuerySnapshot = task.QuerySnapshot,
                Columns = columns,
                UserId = userId,
                TenantId = tenantId
            };

            using var buffer = new MemoryStream();
            var lastNotified = 0;
            await writer.WriteAsync(buffer, columns, provider.ReadRowsAsync(context, cancellationToken), async processed =>
            {
                var total = context.Total;
                var progress = total is > 0 ? (int)Math.Min(99L, processed * 100L / total.Value) : 50;
                await _repository.UpdateProgressAsync(task.BasicId, processed, progress, cancellationToken);

                if (processed - lastNotified >= 1000 || progress >= 99)
                {
                    lastNotified = processed;
                    var detail = total is > 0 ? $"{processed}/{total} 行" : $"{processed} 行";
                    await _notifier.NotifyRunningAsync(userId, islandTaskId, $"正在导出 {task.TaskName}", detail, progress, cancellationToken);
                }
            }, cancellationToken);

            buffer.Position = 0;
            var totalRows = Math.Max(context.Total ?? 0, 0);
            var fileName = BuildFileName(task, actualFormat);
            var (fileId, fileSize) = await UploadAsync(buffer, fileName, task.TaskName, actualFormat, cancellationToken);

            await _repository.MarkSuccessAsync(task.BasicId, fileId, fileName, fileSize, totalRows, DateTimeOffset.UtcNow, cancellationToken);
            await _notifier.NotifySucceededAsync(userId, islandTaskId, $"{task.TaskName} 导出完成", $"共 {totalRows} 行，可在导出中心下载", "/setting/export-center", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await FailAsync(task, userId, islandTaskId, "任务已取消或服务停止", CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出任务执行失败：{TaskId}", task.BasicId);
            await FailAsync(task, userId, islandTaskId, ex.Message, CancellationToken.None);
        }
    }

    private static List<ExportColumnDto> DeserializeColumns(string fieldsSnapshot)
    {
        if (string.IsNullOrWhiteSpace(fieldsSnapshot))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ExportColumnDto>>(fieldsSnapshot) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string BuildFileName(SysExportTask task, ExportFormat format)
    {
        var name = task.TaskName;
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }
        return $"{name}.{GetExtension(format)}";
    }

    private static string GetExtension(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Xlsx => "xlsx",
            _ => "csv"
        };
    }

    private static string GetContentType(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv; charset=utf-8"
        };
    }

    private static ClaimsPrincipal BuildPrincipal(long userId, long tenantId)
    {
        var identity = new ClaimsIdentity("ExportTask");
        identity.AddClaim(new Claim(XiHanClaimTypes.UserId, userId.ToString()));
        identity.AddClaim(new Claim(XiHanClaimTypes.TenantId, tenantId.ToString()));
        return new ClaimsPrincipal(identity);
    }

    private async Task FailAsync(SysExportTask task, long userId, string islandTaskId, string message, CancellationToken cancellationToken)
    {
        var trimmed = message.Length > 1000 ? message[..1000] : message;
        await _repository.MarkFailedAsync(task.BasicId, trimmed, DateTimeOffset.UtcNow, cancellationToken);
        await _notifier.NotifyFailedAsync(userId, islandTaskId, $"{task.TaskName} 导出失败", trimmed, cancellationToken);
    }

    private async Task<(long FileId, long FileSize)> UploadAsync(MemoryStream content, string fileName, string taskName, ExportFormat format, CancellationToken cancellationToken)
    {
        var bytes = content.ToArray();
        var stream = new MemoryStream(bytes);
        var formFile = new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = GetContentType(format)
        };

        var uploadDto = new FileUploadDto
        {
            File = formFile,
            IsTemporary = true,
            RetentionDays = 30,
            Remark = $"导出产物：{taskName}"
        };

        var result = await _fileTransferService.UploadAsync(uploadDto, cancellationToken);
        return (result.File.BasicId, result.File.FileSize);
    }
}
