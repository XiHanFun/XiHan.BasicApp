// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenDatasources;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Queries;

/// <summary>
/// 数据源的列表查询。
/// </summary>
/// <remarks>
/// 关键字匹配名称、描述、主机与库名（照 4.x 页面的搜索提示；4.x 后端只匹配名称与描述）；筛选：数据库类型、状态（<c>eq</c> / <c>in</c>，4.x 前端是多选）、是否缺省（<c>eq</c>）、名称（<c>contains</c>）；
/// 排序：名称、数据库类型、主机、库名、连接结果、状态、排序号、创建时间，没给即排序号再名称升序（照 4.x）。别的筛选与排序是 <c>CODEGENERATION_VALIDATION_001</c>。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
public sealed class CodegenDatasourceQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldSourceName = "sourceName";
    private const string FieldDatabaseType = "databaseType";
    private const string FieldHost = "host";
    private const string FieldDatabaseName = "databaseName";
    private const string FieldLastTestResult = "lastTestResult";
    private const string FieldIsDefault = "isDefault";
    private const string FieldStatus = "status";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出数据源。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    public async ValueTask<Result<PageResult<CodegenDatasourceResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysCodeGenDataSource>(unitOfWork.ConnectionFor(typeof(SysCodeGenDataSource)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(s => s.SourceName.Contains(keyword) || (s.SourceDescription != null && s.SourceDescription.Contains(keyword)) || s.Host.Contains(keyword) || s.DatabaseName.Contains(keyword));
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
            query = query.OrderBy(s => s.Order, OrderByType.Asc).OrderBy(s => s.SourceName, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldSourceName => query.OrderBy(s => s.SourceName, direction),
                FieldDatabaseType => query.OrderBy(s => s.DatabaseType, direction),
                FieldHost => query.OrderBy(s => s.Host, direction),
                FieldDatabaseName => query.OrderBy(s => s.DatabaseName, direction),
                FieldLastTestResult => query.OrderBy(s => s.LastTestResult, direction),
                FieldStatus => query.OrderBy(s => s.Status, direction),
                FieldOrder => query.OrderBy(s => s.Order, direction),
                FieldCreatedAt => query.OrderBy(s => s.CreatedAt, direction),
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
        return new PageResult<CodegenDatasourceResponse>(rows.Select(CodegenDatasourceResponse.From).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 全部数据源，按排序号与名称。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    public async Task<IReadOnlyList<CodegenDatasourceResponse>> OptionsAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rows = await TenantScopedQuery.For<SysCodeGenDataSource>(unitOfWork.ConnectionFor(typeof(SysCodeGenDataSource)), context)
            .OrderBy(s => s.Order, OrderByType.Asc)
            .OrderBy(s => s.SourceName, OrderByType.Asc)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return [.. rows.Select(CodegenDatasourceResponse.From)];
    }

    /// <summary>
    /// 逗号分隔的枚举值。
    /// </summary>
    /// <returns>都认得为列表，否则为 <see langword="null"/>。</returns>
    internal static List<TEnum>? EnumsOf<TEnum>(string value)
        where TEnum : struct, Enum
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new List<TEnum>(parts.Length);
        foreach (var part in parts)
        {
            if (!Enum.TryParse<TEnum>(part, ignoreCase: true, out var item) || !Enum.IsDefined(item))
            {
                return null;
            }

            parsed.Add(item);
        }

        return parsed.Count == 0 ? null : parsed;
    }

    private static Result<ISugarQueryable<SysCodeGenDataSource>> Apply(ISugarQueryable<SysCodeGenDataSource> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldSourceName, "contains") => Result.Success(query.Where(s => s.SourceName.Contains(value))),
            (FieldDatabaseType, "eq" or "in") when EnumsOf<CodeGenDatabaseType>(value) is { } types => Result.Success(query.Where(s => types.Contains(s.DatabaseType))),
            (FieldStatus, "eq" or "in") when EnumsOf<CodeGenStatus>(value) is { } statuses => Result.Success(query.Where(s => statuses.Contains(s.Status))),
            (FieldIsDefault, "eq") when bool.TryParse(value, out var isDefault) => Result.Success(query.Where(s => s.IsDefault == isDefault)),
            _ => Error.Validation(CodeGenerationErrors.QueryUnsupported, CodeGenerationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
