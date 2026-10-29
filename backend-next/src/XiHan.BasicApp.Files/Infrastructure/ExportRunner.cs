// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;
using XiHan.Framework.Security.Abstractions.Authentication;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 一个待执行的导出：任务、所在的侧与发起人这次调用的凭据（只在内存里，不落库）。
/// </summary>
/// <param name="TaskId">任务 ID。</param>
/// <param name="Side">任务所在的侧。</param>
/// <param name="Credential">发起人的凭据。</param>
public sealed record ExportJob(Guid TaskId, string Side, AuthenticationCredential Credential);

/// <summary>
/// 导出队列（进程内）：提交时入队，后台循环取出执行。进程重启时队列里的作业随之丢失，对应任务在启动时记为中断。
/// </summary>
public sealed class ExportJobs
{
    private readonly Channel<ExportJob> _channel = Channel.CreateUnbounded<ExportJob>(new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    /// <summary>
    /// 入队。
    /// </summary>
    /// <param name="job">作业。</param>
    /// <returns>入队成功为 <see langword="true"/>（队列已关闭为 <see langword="false"/>）。</returns>
    public bool Enqueue(ExportJob job) => _channel.Writer.TryWrite(job);

    /// <summary>
    /// 依次取出作业，直到取消。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>作业。</returns>
    public IAsyncEnumerable<ExportJob> ReadAllAsync(CancellationToken cancellationToken) => _channel.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>
/// 导出执行（4.x 同至多并发 2）：以发起人的凭据新开调用、经全管道逐页调列表操作（授权、租户、数据范围与字段安全都与发起人自己查询一致），
/// 行随到随写进临时文件，写完经 <see cref="FileService"/> 存成临时文件（7 天到期，访问级别「已登录」），任务记下结果文件。
/// 凭据过期或会话吊销即失败；失败与中断经独立的短事务记到任务上（那时发起人的凭据可能已经用不了）。
/// </summary>
/// <param name="queue">导出队列。</param>
/// <param name="scopes">作用域工厂。</param>
/// <param name="app">应用（执行管道）。</param>
/// <param name="clock">时钟。</param>
/// <param name="logger">日志。</param>
public sealed partial class ExportRunner(ExportJobs queue, IServiceScopeFactory scopes, App app, TimeProvider clock, ILogger<ExportRunner> logger)
{
    /// <summary>
    /// 一次取多少行（列表操作的页大小上限）。
    /// </summary>
    public const int PageSize = 500;

    /// <summary>
    /// 一次导出至多多少行（4.x 同）。
    /// </summary>
    public const long MaxRows = 1_000_000;

    /// <summary>
    /// 结果文件保留多久。
    /// </summary>
    public static readonly TimeSpan ResultLifetime = TimeSpan.FromDays(7);

    /// <summary>
    /// 后台循环：并发执行队列里的作业，直到取消。
    /// </summary>
    /// <param name="cancellationToken">取消令牌（应用停止）。</param>
    /// <returns>循环结束。</returns>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Parallel.ForEachAsync(queue.ReadAllAsync(cancellationToken), new ParallelOptions { MaxDegreeOfParallelism = 2, CancellationToken = cancellationToken }, async (job, token) =>
            {
                try
                {
                    await ExecuteAsync(job, token).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    LogExportCrashed(logger, exception, job.TaskId);
                    await FailAsync(job.TaskId, exception.Message, CancellationToken.None).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 应用停止：未完成的任务下次启动时记为中断
        }
    }

    /// <summary>
    /// 启动时把上次没执行完的任务（排队中、执行中）记为中断：它们的凭据只在上个进程的内存里。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>结果。</returns>
    public Task<Result> InterruptLeftoversAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        return StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysExportTask)).Updateable<SysExportTask>()
                .SetColumns(t => new SysExportTask { Status = ExportTaskStatus.Failed, ErrorMessage = "interrupted", FinishedAt = now })
                .Where(t => !t.IsDeleted && (t.Status == ExportTaskStatus.Pending || t.Status == ExportTaskStatus.Processing))
                .ExecuteCommandAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }, cancellationToken);
    }

    private async Task ExecuteAsync(ExportJob job, CancellationToken cancellationToken)
    {
        var claimed = await ClaimAsync(job, cancellationToken).ConfigureAwait(false);
        if (claimed is null)
        {
            return;
        }

        var (task, columns) = claimed.Value;
        var temp = Path.Combine(Path.GetTempPath(), "xihan-export-" + Guid.NewGuid().ToString("N"));
        try
        {
            var written = await WriteAsync(job, task, columns, temp, cancellationToken).ConfigureAwait(false);
            if (written.IsFailure)
            {
                await FailAsync(job.TaskId, written.Error!.Message, cancellationToken).ConfigureAwait(false);
                return;
            }

            var saved = await SaveAsync(job, task, temp, cancellationToken).ConfigureAwait(false);
            if (saved.IsFailure)
            {
                await FailAsync(job.TaskId, saved.Error!.Message, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }

    /// <summary>
    /// 领取：任务可能还没随提交调用一起提交，读不到时稍等再读；只领排队中的（取消的不再执行）。
    /// </summary>
    private async Task<(SysExportTask Task, IReadOnlyList<ExportColumn> Columns)?> ClaimAsync(ExportJob job, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            SysExportTask? found = null;
            var outcome = await InCallAsync(job, async context =>
            {
                var rows = context.Services.GetRequiredService<FileRows>();
                if (!(await rows.FindAsync<SysExportTask>(job.Side, job.TaskId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var task))
                {
                    return Result.Success();
                }

                if (task.Status == ExportTaskStatus.Pending)
                {
                    task.Status = ExportTaskStatus.Processing;
                    task.StartedAt = clock.GetUtcNow();
                    await rows.UpdateAsync(task, context.CancellationToken).ConfigureAwait(false);
                }

                found = task;
                return Result.Success();
            }, cancellationToken).ConfigureAwait(false);
            if (outcome.IsFailure)
            {
                await FailAsync(job.TaskId, outcome.Error!.Message, cancellationToken).ConfigureAwait(false);
                return null;
            }

            if (found is not null)
            {
                return found.Status == ExportTaskStatus.Processing && found.StartedAt is not null ? (found, ExportColumn.Parse(found.FieldsSnapshot)) : null;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        }

        LogTaskNotFound(logger, job.TaskId);
        return null;
    }

    private async Task<Result> WriteAsync(ExportJob job, SysExportTask task, IReadOnlyList<ExportColumn> columns, string temp, CancellationToken cancellationToken)
    {
        var query = (string.IsNullOrWhiteSpace(task.QuerySnapshot) ? null : JsonNode.Parse(task.QuerySnapshot) as JsonObject) ?? [];
        if (task.Scope == ExportScope.All)
        {
            query["filters"] = new JsonArray();
            query["keyword"] = null;
        }

        var single = task.Scope == ExportScope.CurrentPage;
        var startIndex = single && query["page"]?["index"]?.GetValue<int>() is { } index and > 0 ? index : 1;
        var size = single && query["page"]?["size"]?.GetValue<int>() is { } given and > 0 ? Math.Min(given, PageSize) : PageSize;
        var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536, FileOptions.Asynchronous);
        await using (output.ConfigureAwait(false))
        {
            var sink = ExportSinks.Create(task.Format, output);
            await using (sink.ConfigureAwait(false))
            {
                await sink.WriteRowAsync([.. columns.Select(static c => c.Title)], cancellationToken).ConfigureAwait(false);
                long processed = 0;
                long total = 0;
                for (var page = startIndex; ; page++)
                {
                    query["page"] = new JsonObject { ["index"] = page, ["size"] = size };
                    var fetched = await PageAsync(job, task.BusinessType, query, cancellationToken).ConfigureAwait(false);
                    if (!fetched.TryGetValue(out var result))
                    {
                        return fetched.Error!;
                    }

                    total = single ? result.Items.Count : Math.Min(result.Total, MaxRows);
                    foreach (var item in result.Items)
                    {
                        if (processed >= MaxRows)
                        {
                            break;
                        }

                        await sink.WriteRowAsync([.. columns.Select(c => c.TextOf(item))], cancellationToken).ConfigureAwait(false);
                        processed++;
                    }

                    var progress = total == 0 ? 100 : (int)Math.Min(99, processed * 100 / total);
                    var reported = await InCallAsync(job, async context =>
                    {
                        var rows = context.Services.GetRequiredService<FileRows>();
                        if ((await rows.FindAsync<SysExportTask>(job.Side, job.TaskId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var current))
                        {
                            current.TotalCount = total;
                            current.ProcessedCount = processed;
                            current.Progress = progress;
                            await rows.UpdateAsync(current, context.CancellationToken).ConfigureAwait(false);
                        }

                        return Result.Success();
                    }, cancellationToken).ConfigureAwait(false);
                    if (reported.IsFailure)
                    {
                        return reported;
                    }

                    if (single || result.Items.Count < size || processed >= total)
                    {
                        break;
                    }
                }

                await sink.CompleteAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return Result.Success();
    }

    private async Task<Result> SaveAsync(ExportJob job, SysExportTask task, string temp, CancellationToken cancellationToken)
    {
        var extension = ExportSinks.ExtensionOf(task.Format);
        var name = string.Concat(task.TaskName.Split(Path.GetInvalidFileNameChars())).Trim();
        var fileName = (name.Length == 0 ? "export" : name[..Math.Min(name.Length, 200)]) + extension;
        return await InCallAsync(job, async context =>
        {
            var files = context.Services.GetRequiredService<FileService>();
            var rows = context.Services.GetRequiredService<FileRows>();
            var source = new FileStream(temp, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan);
            await using (source.ConfigureAwait(false))
            {
                var details = new FileDetails(FileAccessLevel.Authenticated, IsTemporary: true, clock.GetUtcNow().Add(ResultLifetime), "export", context.Localize(FilesMessages.ExportResultRemark, new Dictionary<string, object?> { ["name"] = task.TaskName }));
                var uploaded = await files.UploadAsync(context, source, fileName, ExportSinks.ContentTypeOf(task.Format), details).ConfigureAwait(false);
                if (!uploaded.TryGetValue(out var file))
                {
                    return uploaded.Error!;
                }

                if (!(await rows.FindAsync<SysExportTask>(job.Side, job.TaskId, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var current))
                {
                    return Result.Success();
                }

                current.Status = ExportTaskStatus.Success;
                current.Progress = 100;
                current.FileId = file.Id;
                current.FileName = file.OriginalName;
                current.FileSize = file.Size;
                current.FinishedAt = clock.GetUtcNow();
                await rows.UpdateAsync(current, context.CancellationToken).ConfigureAwait(false);
                return Result.Success();
            }
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 以发起人的身份调一页列表操作（全管道），取出 <c>items</c> 与 <c>total</c>。
    /// </summary>
    private async Task<Result<(IReadOnlyList<JsonElement> Items, long Total)>> PageAsync(ExportJob job, string operationId, JsonObject query, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var context = App.CreateContext(scope.ServiceProvider, cancellationToken);
        context.SetCredential(job.Credential);
        var invoker = scope.ServiceProvider.GetRequiredService<IOperationInvoker>();
        var response = await invoker.InvokeAsync(context, new OperationRequest(operationId, Encoding.UTF8.GetBytes(query.ToJsonString()))).ConfigureAwait(false);
        if (!response.IsSuccess)
        {
            return response.Outcome.Error!.WithText(context.Localize(response.Outcome.Error!));
        }

        using var document = JsonDocument.Parse(response.Payload);
        var root = document.RootElement;
        var items = root.TryGetProperty("items", out var array) && array.ValueKind == JsonValueKind.Array ? [.. array.EnumerateArray().Select(static e => e.Clone())] : new List<JsonElement>();
        var total = root.TryGetProperty("total", out var count) && count.TryGetInt64(out var value) ? value : items.Count;
        return (items, total);
    }

    /// <summary>
    /// 以发起人的身份开一次调用（全管道、自带工作单元）执行 <paramref name="action"/>；失败时错误信息按默认区域翻译。
    /// </summary>
    private async Task<Result> InCallAsync(ExportJob job, Func<InvocationContext, Task<Result>> action, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var context = App.CreateContext(scope.ServiceProvider, cancellationToken);
        context.SetCredential(job.Credential);
        var outcome = await app.InvokeAsync(context, async inner => inner.SetOutcome(await action(inner).ConfigureAwait(false))).ConfigureAwait(false);
        if (outcome.IsFailure)
        {
            return outcome.Error!.WithText(context.Localize(outcome.Error!));
        }

        return outcome;
    }

    /// <summary>
    /// 记失败（独立的短事务，按 ID 改，不依赖发起人的凭据）。
    /// </summary>
    private async Task FailAsync(Guid taskId, string message, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var reason = message.Length <= 1000 ? message : message[..1000];
        var written = await StoreScope.WriteAsync(scopes, async (services, ct) =>
        {
            var unitOfWork = services.GetRequiredService<SqlSugarUnitOfWork>();
            await unitOfWork.EnsureTransactionAsync(ct).ConfigureAwait(false);
            await unitOfWork.ConnectionFor(typeof(SysExportTask)).Updateable<SysExportTask>()
                .SetColumns(t => new SysExportTask { Status = ExportTaskStatus.Failed, ErrorMessage = reason, FinishedAt = now })
                .Where(t => t.Id == taskId && t.Status != ExportTaskStatus.Success)
                .ExecuteCommandAsync(ct).ConfigureAwait(false);
            return Result.Success();
        }, cancellationToken).ConfigureAwait(false);
        if (written.IsFailure)
        {
            LogFailNotRecorded(logger, taskId, written.Error!.Code);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Export task {TaskId} crashed.")]
    private static partial void LogExportCrashed(ILogger logger, Exception exception, Guid taskId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Export task {TaskId} was never committed by its submitting call; the job is dropped.")]
    private static partial void LogTaskNotFound(ILogger logger, Guid taskId);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "The failure of export task {TaskId} was not recorded ({Code}).")]
    private static partial void LogFailNotRecorded(ILogger logger, Guid taskId, string code);
}
