// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenHistories;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Queries;

/// <summary>
/// 生成历史的列表查询
/// </summary>
/// <remarks>
/// 关键字匹配表名与批次号（照 4.x 的表名搜索）；筛选：表配置（<c>eq</c>）、结果与交付方式（<c>eq</c> / <c>in</c>，4.x 前端是多选）、生成时间（<c>ge</c> / <c>le</c>）；
/// 排序：表名、批次号、结果、交付方式、文件数、大小、耗时、操作人、生成时间，没给即生成时间降序。别的筛选与排序是 <c>CODEGENERATION_VALIDATION_001</c>
/// </remarks>
/// <param name="unitOfWork">工作单元</param>
public sealed class CodegenHistoryQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldTableId = "tableId";
    private const string FieldTableName = "tableName";
    private const string FieldBatchNumber = "batchNumber";
    private const string FieldGenerationStatus = "generationStatus";
    private const string FieldDeliveryType = "deliveryType";
    private const string FieldFileCount = "fileCount";
    private const string FieldTotalSize = "totalSize";
    private const string FieldDuration = "duration";
    private const string FieldOperatorName = "operatorName";
    private const string FieldGeneratedAt = "generatedAt";

    /// <summary>
    /// 分页列出生成历史
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="context">调用上下文</param>
    public async ValueTask<Result<PageResult<CodegenHistoryResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysCodeGenHistory>(unitOfWork.ConnectionFor(typeof(SysCodeGenHistory)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(h => h.TableName.Contains(keyword) || (h.BatchNumber != null && h.BatchNumber.Contains(keyword)));
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
            query = query.OrderBy(h => h.GeneratedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldTableName => query.OrderBy(h => h.TableName, direction),
                FieldBatchNumber => query.OrderBy(h => h.BatchNumber, direction),
                FieldGenerationStatus => query.OrderBy(h => h.GenerationStatus, direction),
                FieldDeliveryType => query.OrderBy(h => h.DeliveryType, direction),
                FieldFileCount => query.OrderBy(h => h.FileCount, direction),
                FieldTotalSize => query.OrderBy(h => h.TotalSize, direction),
                FieldDuration => query.OrderBy(h => h.Duration, direction),
                FieldOperatorName => query.OrderBy(h => h.OperatorName, direction),
                FieldGeneratedAt => query.OrderBy(h => h.GeneratedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(CodeGenerationErrors.QueryUnsupported, CodeGenerationMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<CodegenHistoryResponse>(rows.Select(CodegenHistoryResponse.From).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 按 ID 取一条生成历史
    /// </summary>
    /// <param name="id">生成历史 ID</param>
    /// <param name="context">调用上下文</param>
    public async Task<Result<SysCodeGenHistory>> FindAsync(Guid id, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var history = await TenantScopedQuery.For<SysCodeGenHistory>(unitOfWork.ConnectionFor(typeof(SysCodeGenHistory)), context)
            .Where(h => h.Id == id)
            .FirstAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return history is null
            ? Error.NotFound(CodeGenerationErrors.HistoryNotFound, CodeGenerationMessages.HistoryNotFound, MessageArguments.Of(("id", id)))
            : history;
    }

    private static Result<ISugarQueryable<SysCodeGenHistory>> Apply(ISugarQueryable<SysCodeGenHistory> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldTableId, "eq") when Guid.TryParse(value, out var tableId) => Result.Success(query.Where(h => h.TableId == tableId)),
            (FieldTableName, "contains") => Result.Success(query.Where(h => h.TableName.Contains(value))),
            (FieldGenerationStatus, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenGenerationStatus>(value) is { } results => Result.Success(query.Where(h => results.Contains(h.GenerationStatus))),
            (FieldDeliveryType, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenDeliveryType>(value) is { } deliveries => Result.Success(query.Where(h => deliveries.Contains(h.DeliveryType))),
            (FieldGeneratedAt, "ge") when TimeOf(value, out var from) => Result.Success(query.Where(h => h.GeneratedAt >= from)),
            (FieldGeneratedAt, "le") when TimeOf(value, out var to) => Result.Success(query.Where(h => h.GeneratedAt <= to)),
            _ => Error.Validation(CodeGenerationErrors.QueryUnsupported, CodeGenerationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }

    private static bool TimeOf(string text, out DateTimeOffset value) => DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value);
}
