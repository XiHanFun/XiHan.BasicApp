// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Files.Queries;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Operations.ExportTasks;

/// <summary>
/// 提交导出。
/// </summary>
/// <remarks>
/// <c>POST /api/export-tasks:submit</c>。只要求登录；要导出的列表操作在执行时按发起人的身份照常判定权限、数据范围与字段安全（4.x 按导出提供方要求的权限码）。
/// </remarks>
/// <param name="service">导出任务服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class SubmitExportTask(ExportTaskService service) : IOperation<SubmitExportTaskRequest, ExportTaskResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "export-tasks.submit";

    /// <inheritdoc />
    public async ValueTask<Result<ExportTaskResponse>> HandleAsync(SubmitExportTaskRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var submitted = await service.SubmitAsync(context, request.ToSubmission()).ConfigureAwait(false);
        if (!submitted.TryGetValue(out var task))
        {
            return submitted.Error!;
        }

        return ExportTaskResponse.From(task);
    }
}

/// <summary>
/// 分页列出自己的导出任务。
/// </summary>
/// <remarks>
/// <c>POST /api/export-tasks:list</c>（4.x 的「我的导出」）。缺省按提交时间倒序。
/// </remarks>
/// <param name="queries">任务查询。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class ListExportTasks(ExportTaskQueries queries) : IOperation<PageRequest, PageResult<ExportTaskResponse>>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "export-tasks.list";

    /// <inheritdoc />
    public ValueTask<Result<PageResult<ExportTaskResponse>>> HandleAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return queries.ListAsync(request, context);
    }
}

/// <summary>
/// 取自己的一个导出任务：<c>GET /api/export-tasks/{id}</c>。
/// </summary>
/// <param name="service">导出任务服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class GetExportTask(ExportTaskService service) : IOperation<ExportTaskIdRequest, ExportTaskResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "export-tasks.get";

    /// <inheritdoc />
    public async ValueTask<Result<ExportTaskResponse>> HandleAsync(ExportTaskIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var task))
        {
            return found.Error!;
        }

        return ExportTaskResponse.From(task);
    }
}

/// <summary>
/// 取消自己排队中的导出。
/// </summary>
/// <remarks>
/// <c>POST /api/export-tasks/{id}:cancel</c>。执行中的不能取消（4.x 同）。
/// </remarks>
/// <param name="service">导出任务服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CancelExportTask(ExportTaskService service) : IOperation<ExportTaskIdRequest, ExportTaskResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "export-tasks.cancel";

    /// <inheritdoc />
    public async ValueTask<Result<ExportTaskResponse>> HandleAsync(ExportTaskIdRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(context, request.Id).ConfigureAwait(false);
        if (!found.TryGetValue(out var task))
        {
            return found.Error!;
        }

        var cancelled = await service.CancelAsync(context, task).ConfigureAwait(false);
        if (cancelled.IsFailure)
        {
            return cancelled.Error!;
        }

        return ExportTaskResponse.From(task);
    }
}

/// <summary>
/// 删除自己的导出任务，结果文件一并删除。
/// </summary>
/// <remarks>
/// <c>DELETE /api/export-tasks?ids=</c>。结果文件一并删除（4.x 留着文件）；执行中的逐项拒绝。
/// </remarks>
/// <param name="service">导出任务服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class DeleteExportTasks(ExportTaskService service) : IOperation<ExportTaskIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "export-tasks.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(ExportTaskIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        return await service.DeleteAsync(context, request.Ids).ConfigureAwait(false);
    }
}
