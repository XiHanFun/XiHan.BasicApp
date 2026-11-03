// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTables;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Queries;

/// <summary>
/// 表配置的列表查询
/// </summary>
/// <remarks>
/// 关键字匹配表名、类名与表注释（照 4.x）；筛选：生成模式、交付方式、生成结果、状态（<c>eq</c> / <c>in</c>，4.x 前端是多选）、生成目标与数据源（<c>eq</c>）、包名（<c>eq</c> / <c>contains</c>）；
/// 排序：表名、类名、表注释、包名、生成模式、交付方式、生成结果、状态、最近生成时间、创建时间，没给即创建时间降序。别的筛选与排序是 <c>CODEGENERATION_VALIDATION_001</c>
/// </remarks>
/// <param name="unitOfWork">工作单元</param>
public sealed class CodegenTableQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldTableName = "tableName";
    private const string FieldClassName = "className";
    private const string FieldTableComment = "tableComment";
    private const string FieldModuleName = "moduleName";
    private const string FieldTemplateType = "templateType";
    private const string FieldDeliveryType = "deliveryType";
    private const string FieldGenerationStatus = "generationStatus";
    private const string FieldTarget = "target";
    private const string FieldDataSourceId = "dataSourceId";
    private const string FieldStatus = "status";
    private const string FieldLastGeneratedAt = "lastGeneratedAt";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出表配置
    /// </summary>
    /// <param name="request">分页请求</param>
    /// <param name="context">调用上下文</param>
    public async ValueTask<Result<PageResult<CodegenTableResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysCodeGenTable>(unitOfWork.ConnectionFor(typeof(SysCodeGenTable)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t => t.TableName.Contains(keyword) || t.ClassName.Contains(keyword) || (t.TableComment != null && t.TableComment.Contains(keyword)));
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
            query = query.OrderBy(t => t.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldTableName => query.OrderBy(t => t.TableName, direction),
                FieldClassName => query.OrderBy(t => t.ClassName, direction),
                FieldTableComment => query.OrderBy(t => t.TableComment, direction),
                FieldModuleName => query.OrderBy(t => t.ModuleName, direction),
                FieldTemplateType => query.OrderBy(t => t.TemplateType, direction),
                FieldDeliveryType => query.OrderBy(t => t.DeliveryType, direction),
                FieldGenerationStatus => query.OrderBy(t => t.GenerationStatus, direction),
                FieldStatus => query.OrderBy(t => t.Status, direction),
                FieldLastGeneratedAt => query.OrderBy(t => t.LastGeneratedAt, direction),
                FieldCreatedAt => query.OrderBy(t => t.CreatedAt, direction),
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
        return new PageResult<CodegenTableResponse>(rows.Select(CodegenTableResponse.From).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 全部表配置，按表名
    /// </summary>
    /// <param name="context">调用上下文</param>
    public async Task<IReadOnlyList<CodegenTableResponse>> OptionsAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rows = await TenantScopedQuery.For<SysCodeGenTable>(unitOfWork.ConnectionFor(typeof(SysCodeGenTable)), context)
            .OrderBy(t => t.TableName, OrderByType.Asc)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);
        return [.. rows.Select(CodegenTableResponse.From)];
    }

    private static Result<ISugarQueryable<SysCodeGenTable>> Apply(ISugarQueryable<SysCodeGenTable> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldTableName, "contains") => Result.Success(query.Where(t => t.TableName.Contains(value))),
            (FieldModuleName, "eq") => Result.Success(query.Where(t => t.ModuleName == value)),
            (FieldModuleName, "contains") => Result.Success(query.Where(t => t.ModuleName != null && t.ModuleName.Contains(value))),
            (FieldTemplateType, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenTemplateType>(value) is { } types => Result.Success(query.Where(t => types.Contains(t.TemplateType))),
            (FieldDeliveryType, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenDeliveryType>(value) is { } deliveries => Result.Success(query.Where(t => deliveries.Contains(t.DeliveryType))),
            (FieldGenerationStatus, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenGenerationStatus>(value) is { } results => Result.Success(query.Where(t => results.Contains(t.GenerationStatus))),
            (FieldStatus, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenStatus>(value) is { } statuses => Result.Success(query.Where(t => statuses.Contains(t.Status))),
            (FieldTarget, "eq") when Enum.TryParse<CodeGenTarget>(value, ignoreCase: true, out var target) && Enum.IsDefined(target) => Result.Success(query.Where(t => t.Target == target)),
            (FieldDataSourceId, "eq") when Guid.TryParse(value, out var source) => Result.Success(query.Where(t => t.DataSourceId == source)),
            _ => Error.Validation(CodeGenerationErrors.QueryUnsupported, CodeGenerationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
