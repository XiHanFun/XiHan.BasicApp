// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.ConstraintRules;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Queries;

/// <summary>
/// 约束规则列表查询。
/// </summary>
/// <remarks>
/// SqlSugar 直查，作用侧 = 当前租户或平台。
/// 关键字匹配编码、名称、说明与备注；
/// 筛选 <c>type / targetType / violationAction / status</c> 的 <c>eq / in</c>；排序 <c>code / name / type / priority / status / createdAt</c>，缺省按优先级从大到小、再按编码（即求值顺序）。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="projection">规则投影。</param>
public sealed class ConstraintRuleQueries(SqlSugarUnitOfWork unitOfWork, ConstraintRuleProjection projection)
{
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldType = "type";
    private const string FieldTargetType = "targetType";
    private const string FieldViolationAction = "violationAction";
    private const string FieldPriority = "priority";
    private const string FieldStatus = "status";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出当前作用侧的规则。
    /// </summary>
    public async ValueTask<Result<PageResult<ConstraintRuleResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysConstraintRule>(unitOfWork.ConnectionFor(typeof(SysConstraintRule)), context).OwnedByCurrentTenant(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.Code.Contains(keyword) || r.Name.Contains(keyword) || (r.Description != null && r.Description.Contains(keyword)) || (r.Remark != null && r.Remark.Contains(keyword)));
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
            query = query.OrderBy(r => r.Priority, OrderByType.Desc).OrderBy(r => r.Code, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(r => r.Code, direction),
                FieldName => query.OrderBy(r => r.Name, direction),
                FieldType => query.OrderBy(r => r.Type, direction),
                FieldPriority => query.OrderBy(r => r.Priority, direction),
                FieldStatus => query.OrderBy(r => r.Status, direction),
                FieldCreatedAt => query.OrderBy(r => r.CreatedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<ConstraintRuleResponse>(await projection.ToAsync(rows, context.CancellationToken).ConfigureAwait(false), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysConstraintRule>> Apply(ISugarQueryable<SysConstraintRule> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldType, "eq" or "in") when EnumsOf<ConstraintType>(value) is { } types => Result.Success(query.Where(r => types.Contains(r.Type))),
            (FieldTargetType, "eq" or "in") when EnumsOf<ConstraintTargetType>(value) is { } targets => Result.Success(query.Where(r => targets.Contains(r.TargetType))),
            (FieldViolationAction, "eq" or "in") when EnumsOf<ConstraintViolationAction>(value) is { } actions => Result.Success(query.Where(r => actions.Contains(r.ViolationAction))),
            (FieldStatus, "eq" or "in") when EnumsOf<ConstraintRuleStatus>(value) is { } statuses => Result.Success(query.Where(r => statuses.Contains(r.Status))),
            _ => Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }

    /// <summary>
    /// 解析逗号分隔的枚举名。
    /// </summary>
    /// <returns>枚举值；有一个认不出即为空（交给不支持的筛选报错）。</returns>
    private static List<TEnum>? EnumsOf<TEnum>(string value) where TEnum : struct, Enum
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
}
