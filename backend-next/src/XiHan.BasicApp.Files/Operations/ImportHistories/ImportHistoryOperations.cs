// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Domain.Services;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Operations.ImportHistories;

/// <summary>
/// <c>import-histories.create</c> 的请求。
/// </summary>
/// <remarks>
/// 4.x 同，计数不小于 0，错误摘要截到 8000 个字符。
/// </remarks>
/// <param name="PageCode">页面码。</param>
/// <param name="FileName">导入的文件名。</param>
/// <param name="TotalCount">总行数。</param>
/// <param name="SuccessCount">成功行数。</param>
/// <param name="FailCount">失败行数。</param>
/// <param name="ResourceCode">资源码。</param>
/// <param name="ErrorSummary">错误摘要（JSON）。</param>
public sealed record CreateImportHistoryRequest(
    [Required, StringLength(100, MinimumLength = 1)] string PageCode,
    [Required, StringLength(256, MinimumLength = 1)] string FileName,
    [Range(0, int.MaxValue)] int TotalCount,
    [Range(0, int.MaxValue)] int SuccessCount,
    [Range(0, int.MaxValue)] int FailCount,
    [StringLength(100)] string? ResourceCode = null,
    string? ErrorSummary = null);

/// <summary>
/// 一条导入历史。
/// </summary>
/// <param name="Id">记录 ID。</param>
/// <param name="PageCode">页面码。</param>
/// <param name="ResourceCode">资源码。</param>
/// <param name="FileName">文件名。</param>
/// <param name="TotalCount">总行数。</param>
/// <param name="SuccessCount">成功行数。</param>
/// <param name="FailCount">失败行数。</param>
/// <param name="ErrorSummary">错误摘要。</param>
/// <param name="CreatedAt">导入时间。</param>
public sealed record ImportHistoryResponse(Guid Id, string PageCode, string? ResourceCode, string FileName, int TotalCount, int SuccessCount, int FailCount, string? ErrorSummary, DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由记录投影。
    /// </summary>
    /// <param name="history">记录。</param>
    /// <returns>响应。</returns>
    public static ImportHistoryResponse From(SysImportHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);
        return new(history.Id, history.PageCode, history.ResourceCode, history.FileName, history.TotalCount, history.SuccessCount, history.FailCount, history.ErrorSummary, history.CreatedAt);
    }
}

/// <summary>
/// <c>import-histories.recent</c> 的请求。
/// </summary>
/// <param name="PageCode">页面码。</param>
/// <param name="Count">条数（1–50，缺省 5）。</param>
public sealed record RecentImportHistoriesRequest([Required, StringLength(100, MinimumLength = 1)] string PageCode, [Range(1, 50)] int Count = 5);

/// <summary>
/// <c>import-histories.recent</c> 的响应。
/// </summary>
/// <param name="Items">最近的记录（新的在前）。</param>
public sealed record ImportHistoriesResponse(IReadOnlyList<ImportHistoryResponse> Items);

/// <summary>
/// 记一次导入的结果。
/// </summary>
/// <remarks>
/// <c>POST /api/import-histories</c>（4.x 同只要求登录；导入本身由前端逐行调资源的新建操作完成）。
/// </remarks>
/// <param name="rows">记录读写。</param>
/// <param name="ids">ID 生成器。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class CreateImportHistory(SideRows rows, IIdGenerator<Guid> ids) : IOperation<CreateImportHistoryRequest, ImportHistoryResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "import-histories.create";

    /// <inheritdoc />
    public async ValueTask<Result<ImportHistoryResponse>> HandleAsync(CreateImportHistoryRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (ExportTaskService.RequesterOf(context) is not { } userId)
        {
            return Error.Unavailable(FilesErrors.ExportCredentialMissing, FilesMessages.ExportCredentialMissing);
        }

        var summary = string.IsNullOrEmpty(request.ErrorSummary) ? null : request.ErrorSummary.Length <= 8000 ? request.ErrorSummary : request.ErrorSummary[..8000];
        var history = new SysImportHistory
        {
            Id = ids.NewId(),
            TenantId = SideRows.SideOf(context),
            UserId = userId,
            PageCode = request.PageCode.Trim(),
            ResourceCode = string.IsNullOrWhiteSpace(request.ResourceCode) ? null : request.ResourceCode.Trim(),
            FileName = request.FileName.Trim(),
            TotalCount = request.TotalCount,
            SuccessCount = request.SuccessCount,
            FailCount = request.FailCount,
            ErrorSummary = summary,
        };
        await rows.AppendAsync(history, context.CancellationToken).ConfigureAwait(false);
        return ImportHistoryResponse.From(history);
    }
}

/// <summary>
/// 列出本人在某页最近的导入。
/// </summary>
/// <remarks>
/// <c>GET /api/import-histories:recent?pageCode=&amp;count=</c>（4.x 同）。
/// </remarks>
/// <param name="rows">记录读写。</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Policy = Policies.Authenticated)]
public sealed class RecentImportHistories(SideRows rows) : IOperation<RecentImportHistoriesRequest, ImportHistoriesResponse>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "import-histories.recent";

    /// <inheritdoc />
    public async ValueTask<Result<ImportHistoriesResponse>> HandleAsync(RecentImportHistoriesRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (ExportTaskService.RequesterOf(context) is not { } userId)
        {
            return new ImportHistoriesResponse([]);
        }

        var pageCode = request.PageCode.Trim();
        var list = await rows.Records<SysImportHistory>(SideRows.SideOf(context))
            .Where(h => h.UserId == userId && h.PageCode == pageCode)
            .OrderBy(h => h.CreatedAt, SqlSugar.OrderByType.Desc)
            .Take(request.Count)
            .ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return new ImportHistoriesResponse([.. list.Select(ImportHistoryResponse.From)]);
    }
}
