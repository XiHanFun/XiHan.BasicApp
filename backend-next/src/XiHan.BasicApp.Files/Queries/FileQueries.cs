// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Files.Domain.Entities;
using XiHan.BasicApp.Files.Infrastructure;
using XiHan.BasicApp.Files.Operations.Files;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Files.Queries;

/// <summary>
/// 文件列表（当前一侧）：关键字匹配原始文件名、系统文件名、扩展名、内容类型与标签；按大类、状态、访问级别多选，按临时、来源、扩展名、内容类型筛选，上传时间与到期时间可按区间；缺省按上传时间倒序（4.x 同）。
/// </summary>
/// <param name="rows">文件读写。</param>
public sealed class FileQueries(FileRows rows)
{
    private const string FieldKind = "kind";
    private const string FieldStatus = "status";
    private const string FieldAccessLevel = "accessLevel";
    private const string FieldIsTemporary = "isTemporary";
    private const string FieldSource = "source";
    private const string FieldExtension = "extension";
    private const string FieldContentType = "contentType";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldExpiresAt = "expiresAt";
    private const string FieldOriginalName = "originalName";
    private const string FieldSize = "size";
    private const string FieldDownloadCount = "downloadCount";

    /// <summary>
    /// 分页列出。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    /// <returns>一页文件。</returns>
    public async ValueTask<Result<PageResult<FileResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = rows.Query<SysFile>(FileRows.SideOf(context));
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(f => f.OriginalName.Contains(keyword) || f.Name.Contains(keyword) || (f.Extension != null && f.Extension.Contains(keyword)) || f.ContentType.Contains(keyword) || (f.Tags != null && f.Tags.Contains(keyword)));
        }

        foreach (var filter in request.Filters)
        {
            var filtered = Apply(query, filter);
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!;
            }
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(f => f.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCreatedAt => query.OrderBy(f => f.CreatedAt, direction),
                FieldOriginalName => query.OrderBy(f => f.OriginalName, direction),
                FieldSize => query.OrderBy(f => f.Size, direction),
                FieldDownloadCount => query.OrderBy(f => f.DownloadCount, direction),
                FieldKind => query.OrderBy(f => f.Kind, direction),
                FieldExpiresAt => query.OrderBy(f => f.ExpiresAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(FilesErrors.QueryUnsupported, FilesMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var page = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<FileResponse>([.. page.Select(FileResponse.From)], total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysFile>> Apply(ISugarQueryable<SysFile> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        var lowered = value.ToLowerInvariant();
        return (filter.Field, filter.Operator) switch
        {
            (FieldKind, "eq" or "in") when Enums.Of<FileKind>(value) is { } kinds => Result.Success(query.Where(f => kinds.Contains(f.Kind))),
            (FieldStatus, "eq" or "in") when Enums.Of<FileStatus>(value) is { } statuses => Result.Success(query.Where(f => statuses.Contains(f.Status))),
            (FieldAccessLevel, "eq" or "in") when Enums.Of<FileAccessLevel>(value) is { } levels => Result.Success(query.Where(f => levels.Contains(f.AccessLevel))),
            (FieldIsTemporary, "eq") when bool.TryParse(value, out var temporary) => Result.Success(query.Where(f => f.IsTemporary == temporary)),
            (FieldSource, "eq") => Result.Success(query.Where(f => f.Source == value)),
            (FieldExtension, "eq") => Result.Success(query.Where(f => f.Extension == lowered)),
            (FieldContentType, "eq") => Result.Success(query.Where(f => f.ContentType == value)),
            (FieldCreatedAt, "ge") when TimeOf(value) is { } from => Result.Success(query.Where(f => f.CreatedAt >= from)),
            (FieldCreatedAt, "le") when TimeOf(value) is { } to => Result.Success(query.Where(f => f.CreatedAt <= to)),
            (FieldExpiresAt, "ge") when TimeOf(value) is { } from => Result.Success(query.Where(f => f.ExpiresAt >= from)),
            (FieldExpiresAt, "le") when TimeOf(value) is { } to => Result.Success(query.Where(f => f.ExpiresAt <= to)),
            _ => Error.Validation(FilesErrors.QueryUnsupported, FilesMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }

    private static DateTimeOffset? TimeOf(string value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time) ? time : null;
}
