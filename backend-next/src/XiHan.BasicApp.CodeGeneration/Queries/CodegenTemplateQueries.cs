// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTemplates;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Queries;

/// <summary>
/// 模板的列表查询。
/// </summary>
/// <remarks>
/// 关键字匹配编码、名称与描述（照 4.x）；筛选：分组（<c>eq</c>）、生成模式、写入方式与状态（<c>eq</c> / <c>in</c>）、是否内置（<c>eq</c>）；
/// 排序：编码、名称、分组、生成模式、状态、排序号、创建时间，没给即排序号再编码升序（照 4.x）。别的筛选与排序是 <c>CODEGENERATION_VALIDATION_001</c>。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
public sealed class CodegenTemplateQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldTemplateCode = "templateCode";
    private const string FieldTemplateName = "templateName";
    private const string FieldTemplateGroup = "templateGroup";
    private const string FieldTemplateType = "templateType";
    private const string FieldWriteMode = "writeMode";
    private const string FieldIsBuiltIn = "isBuiltIn";
    private const string FieldStatus = "status";
    private const string FieldOrder = "order";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出模板。
    /// </summary>
    /// <param name="request">分页请求。</param>
    /// <param name="context">调用上下文。</param>
    public async ValueTask<Result<PageResult<CodegenTemplateResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysCodeGenTemplate>(unitOfWork.ConnectionFor(typeof(SysCodeGenTemplate)), context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(t => t.TemplateCode.Contains(keyword) || t.TemplateName.Contains(keyword) || (t.TemplateDescription != null && t.TemplateDescription.Contains(keyword)));
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
            query = query.OrderBy(t => t.Order, OrderByType.Asc).OrderBy(t => t.TemplateCode, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldTemplateCode => query.OrderBy(t => t.TemplateCode, direction),
                FieldTemplateName => query.OrderBy(t => t.TemplateName, direction),
                FieldTemplateGroup => query.OrderBy(t => t.TemplateGroup, direction),
                FieldTemplateType => query.OrderBy(t => t.TemplateType, direction),
                FieldStatus => query.OrderBy(t => t.Status, direction),
                FieldOrder => query.OrderBy(t => t.Order, direction),
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
        return new PageResult<CodegenTemplateResponse>(rows.Select(CodegenTemplateResponse.From).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysCodeGenTemplate>> Apply(ISugarQueryable<SysCodeGenTemplate> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldTemplateGroup, "eq") => Result.Success(query.Where(t => t.TemplateGroup == value)),
            (FieldTemplateType, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenTemplateType>(value) is { } types => Result.Success(query.Where(t => types.Contains(t.TemplateType))),
            (FieldWriteMode, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenWriteMode>(value) is { } modes => Result.Success(query.Where(t => modes.Contains(t.WriteMode))),
            (FieldStatus, "eq" or "in") when CodegenDatasourceQueries.EnumsOf<CodeGenStatus>(value) is { } statuses => Result.Success(query.Where(t => statuses.Contains(t.Status))),
            (FieldIsBuiltIn, "eq") when bool.TryParse(value, out var builtIn) => Result.Success(query.Where(t => t.IsBuiltIn == builtIn)),
            _ => Error.Validation(CodeGenerationErrors.QueryUnsupported, CodeGenerationMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }
}
