// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Operations.Policies;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Queries;

/// <summary>
/// 访问策略列表查询。
/// </summary>
/// <remarks>
/// SqlSugar 直查，作用侧 = 当前租户或平台。
/// 关键字匹配编码、名称与说明；
/// 筛选 <c>action / status</c> 的 <c>eq / in</c> 与 <c>code</c> 的 <c>eq / contains</c>；排序 <c>code / name / action / priority / status / createdAt / modifiedAt</c>，缺省按优先级升序、再按编码（即求值顺序）。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="projection">策略投影。</param>
public sealed class AccessPolicyQueries(SqlSugarUnitOfWork unitOfWork, AccessPolicyProjection projection)
{
    private const string FieldCode = "code";
    private const string FieldName = "name";
    private const string FieldAction = "action";
    private const string FieldPriority = "priority";
    private const string FieldStatus = "status";
    private const string FieldCreatedAt = "createdAt";
    private const string FieldModifiedAt = "modifiedAt";

    /// <summary>
    /// 分页列出当前作用侧的策略。
    /// </summary>
    public async ValueTask<Result<PageResult<AccessPolicyResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysAccessPolicy>(unitOfWork.ConnectionFor(typeof(SysAccessPolicy)), context).OwnedByCurrentTenant(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(p => p.Code.Contains(keyword) || p.Name.Contains(keyword) || (p.Description != null && p.Description.Contains(keyword)));
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
            query = query.OrderBy(p => p.Priority, OrderByType.Asc).OrderBy(p => p.Code, OrderByType.Asc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldCode => query.OrderBy(p => p.Code, direction),
                FieldName => query.OrderBy(p => p.Name, direction),
                FieldAction => query.OrderBy(p => p.Action, direction),
                FieldPriority => query.OrderBy(p => p.Priority, direction),
                FieldStatus => query.OrderBy(p => p.Status, direction),
                FieldCreatedAt => query.OrderBy(p => p.CreatedAt, direction),
                FieldModifiedAt => query.OrderBy(p => p.ModifiedAt, direction),
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
        var items = await projection.ToAsync(rows, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<AccessPolicyResponse>(items, total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysAccessPolicy>> Apply(ISugarQueryable<SysAccessPolicy> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        return (filter.Field, filter.Operator) switch
        {
            (FieldCode, "eq") => Result.Success(query.Where(p => p.Code == value)),
            (FieldCode, "contains") => Result.Success(query.Where(p => p.Code.Contains(value))),
            (FieldAction, "eq" or "in") when EnumsOf<AccessPolicyAction>(value) is { } actions => Result.Success(query.Where(p => actions.Contains(p.Action))),
            (FieldStatus, "eq" or "in") when EnumsOf<AccessPolicyStatus>(value) is { } statuses => Result.Success(query.Where(p => statuses.Contains(p.Status))),
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
