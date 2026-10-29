// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Nodes;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Files.Domain.Services;

/// <summary>
/// 一次导出提交的内容。
/// </summary>
/// <param name="BusinessType">要导出的列表操作 ID。</param>
/// <param name="TaskName">任务名称；为空按「操作 ID_时间」起名（4.x 同）。</param>
/// <param name="Scope">范围。</param>
/// <param name="Format">格式。</param>
/// <param name="QuerySnapshot">查询快照（分页请求 JSON）。</param>
/// <param name="FieldsSnapshot">列快照（JSON）。</param>
public sealed record ExportSubmission(string BusinessType, string? TaskName, ExportScope Scope, ExportFormat Format, string? QuerySnapshot, string FieldsSnapshot);

/// <summary>
/// 导出任务（4.x 同）：提交写一条排队中的任务并入队，执行由 <see cref="ExportRunner"/> 在后台以发起人的身份完成；每人只看得到自己的任务。
/// 与 4.x 不同：要导出的是某个 <c>.list</c> 操作本身（不另写导出提供方），权限、数据范围与字段安全由那个操作照常判定；删除任务连同结果文件。
/// </summary>
/// <param name="rows">任务读写。</param>
/// <param name="files">文件服务（删除结果文件）。</param>
/// <param name="queue">导出队列。</param>
/// <param name="registry">操作登记表。</param>
/// <param name="ids">ID 生成器。</param>
/// <param name="clock">时钟。</param>
public sealed class ExportTaskService(FileRows rows, FileService files, ExportJobs queue, OperationRegistry registry, IIdGenerator<Guid> ids, TimeProvider clock)
{
    /// <summary>
    /// 调用方的用户 ID；没有登录用户为空。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>用户 ID。</returns>
    public static Guid? RequesterOf(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Guid.TryParse(context.GetSecurity().Principal.Id, out var userId) ? userId : null;
    }

    /// <summary>
    /// 提交：校验要导出的列表操作与列，写一条排队中的任务，把发起人这次调用的凭据带进作业入队。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="submission">提交内容。</param>
    /// <returns>新任务。</returns>
    public async Task<Result<SysExportTask>> SubmitAsync(InvocationContext context, ExportSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(submission);
        var businessType = submission.BusinessType.Trim();
        if (!businessType.EndsWith(".list", StringComparison.Ordinal) || !registry.Find(businessType).HasValue)
        {
            return Error.Validation(FilesErrors.ExportTargetInvalid, FilesMessages.ExportTargetInvalid, MessageArguments.Of(("businessType", businessType)));
        }

        if (!string.IsNullOrWhiteSpace(submission.QuerySnapshot) && !IsJsonObject(submission.QuerySnapshot))
        {
            return Error.Validation(FilesErrors.ExportQueryInvalid, FilesMessages.ExportQueryInvalid);
        }

        if (ExportColumn.Parse(submission.FieldsSnapshot).Count == 0)
        {
            return Error.Validation(FilesErrors.ExportColumnsRequired, FilesMessages.ExportColumnsRequired);
        }

        if (!context.GetCredential().TryGetValue(out var credential) || RequesterOf(context) is not { } requester)
        {
            return Error.Unavailable(FilesErrors.ExportCredentialMissing, FilesMessages.ExportCredentialMissing);
        }

        var side = FileRows.SideOf(context);
        var name = submission.TaskName?.Trim();
        var task = new SysExportTask
        {
            Id = ids.NewId(),
            TenantId = side,
            BusinessType = businessType,
            TaskName = string.IsNullOrEmpty(name) ? $"{businessType}_{clock.GetUtcNow():yyyyMMddHHmmss}" : name,
            Scope = submission.Scope,
            Format = submission.Format,
            Status = ExportTaskStatus.Pending,
            QuerySnapshot = string.IsNullOrWhiteSpace(submission.QuerySnapshot) ? null : submission.QuerySnapshot,
            FieldsSnapshot = submission.FieldsSnapshot,
            RequesterId = requester,
        };
        await rows.InsertAsync(task, context.CancellationToken).ConfigureAwait(false);
        queue.Enqueue(new ExportJob(task.Id, side, credential));
        return task;
    }

    /// <summary>
    /// 调用方自己的一个任务。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="id">任务 ID。</param>
    /// <returns>任务；不存在或不是自己的为失败。</returns>
    public async Task<Result<SysExportTask>> FindAsync(InvocationContext context, Guid id)
    {
        ArgumentNullException.ThrowIfNull(context);
        var requester = RequesterOf(context);
        if (requester is not null && (await rows.FindAsync<SysExportTask>(FileRows.SideOf(context), id, context.CancellationToken).ConfigureAwait(false)).TryGetValue(out var task) && task.RequesterId == requester)
        {
            return task;
        }

        return Error.NotFound(FilesErrors.ExportTaskNotFound, FilesMessages.ExportTaskNotFound, MessageArguments.Of(("id", id)));
    }

    /// <summary>
    /// 取消：只在排队中，记为失败并写明已取消（4.x 同）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="task">任务。</param>
    /// <returns>结果。</returns>
    public async Task<Result> CancelAsync(InvocationContext context, SysExportTask task)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(task);
        if (task.Status != ExportTaskStatus.Pending)
        {
            return Error.Conflict(FilesErrors.ExportNotPending, FilesMessages.ExportNotPending, MessageArguments.Of(("status", task.Status)));
        }

        task.Status = ExportTaskStatus.Failed;
        task.ErrorMessage = context.Localize(FilesMessages.ExportCancelled);
        task.FinishedAt = clock.GetUtcNow();
        await rows.UpdateAsync(task, context.CancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <summary>
    /// 删除自己的任务，连同结果文件（执行中的任务等它结束）。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="taskIds">任务 ID。</param>
    /// <returns>逐项结果。</returns>
    public async Task<BatchResult> DeleteAsync(InvocationContext context, IReadOnlyList<Guid> taskIds)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(taskIds);
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in taskIds.Distinct())
        {
            var found = await FindAsync(context, id).ConfigureAwait(false);
            if (!found.TryGetValue(out var task))
            {
                failed.Add(new BatchFailure(id, found.Error!.Code));
                continue;
            }

            if (task.Status == ExportTaskStatus.Processing)
            {
                failed.Add(new BatchFailure(id, FilesErrors.ExportNotPending));
                continue;
            }

            if (task.FileId is { } fileId)
            {
                await files.DeleteAsync(task.TenantId, [fileId], context.CancellationToken).ConfigureAwait(false);
            }

            await rows.SoftDeleteAsync(task, context.CancellationToken).ConfigureAwait(false);
            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }

    private static bool IsJsonObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) is JsonObject;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
