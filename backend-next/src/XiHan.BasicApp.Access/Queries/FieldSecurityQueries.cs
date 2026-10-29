// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Access.Operations.FieldSecurity;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Queries;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Queries;

/// <summary>
/// 字段安全规则列表查询。
/// </summary>
/// <remarks>
/// SqlSugar 直查，作用侧 = 当前租户或平台。
/// 关键字匹配字段名与备注；
/// 筛选 <c>entity / targetKind / strategy / status</c> 的 <c>eq / in</c> 与 <c>field</c> 的 <c>eq / contains</c>；排序 <c>entity / field / targetKind / strategy / isEditable / status / createdAt</c>，缺省按创建时间倒序。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="service">字段安全规则服务（补目标显示名）。</param>
public sealed class FieldSecurityQueries(SqlSugarUnitOfWork unitOfWork, FieldSecurityService service)
{
    private const string FieldEntity = "entity";
    private const string FieldField = "field";
    private const string FieldTargetKind = "targetKind";
    private const string FieldStrategy = "strategy";
    private const string FieldIsEditable = "isEditable";
    private const string FieldStatus = "status";
    private const string FieldCreatedAt = "createdAt";

    /// <summary>
    /// 分页列出当前作用侧的规则。
    /// </summary>
    public async ValueTask<Result<PageResult<FieldSecurityRuleResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = TenantScopedQuery.For<SysFieldSecurityRule>(unitOfWork.ConnectionFor(typeof(SysFieldSecurityRule)), context).OwnedByCurrentTenant(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.Field.Contains(keyword) || (r.Remark != null && r.Remark.Contains(keyword)));
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
            query = query.OrderBy(r => r.CreatedAt, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldEntity => query.OrderBy(r => r.Entity, direction),
                FieldField => query.OrderBy(r => r.Field, direction),
                FieldTargetKind => query.OrderBy(r => r.TargetKind, direction),
                FieldStrategy => query.OrderBy(r => r.Strategy, direction),
                FieldIsEditable => query.OrderBy(r => r.IsEditable, direction),
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
        var names = await service.TargetNamesAsync(rows, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<FieldSecurityRuleResponse>(rows.Select(r => FieldSecurityRuleResponse.From(r, names.GetValueOrDefault((r.TargetKind, r.TargetId)))).ToList(), total.Value, request.Page);
    }

    private static Result<ISugarQueryable<SysFieldSecurityRule>> Apply(ISugarQueryable<SysFieldSecurityRule> query, PageFilter filter)
    {
        var value = filter.Value ?? string.Empty;
        var values = SplitOf(value);
        return (filter.Field, filter.Operator) switch
        {
            (FieldEntity, "eq") => Result.Success(query.Where(r => r.Entity == value)),
            (FieldEntity, "in") when values.Count > 0 => Result.Success(query.Where(r => values.Contains(r.Entity))),
            (FieldField, "eq") => Result.Success(query.Where(r => r.Field == value)),
            (FieldField, "contains") => Result.Success(query.Where(r => r.Field.Contains(value))),
            (FieldTargetKind, "eq" or "in") when EnumsOf<FieldSecurityTargetKind>(value) is { } kinds => Result.Success(query.Where(r => kinds.Contains(r.TargetKind))),
            (FieldStrategy, "eq" or "in") when EnumsOf<FieldMaskStrategy>(value) is { } strategies => Result.Success(query.Where(r => strategies.Contains(r.Strategy))),
            (FieldStatus, "eq" or "in") when EnumsOf<FieldRuleStatus>(value) is { } statuses => Result.Success(query.Where(r => statuses.Contains(r.Status))),
            _ => Error.Validation(AccessErrors.QueryUnsupported, AccessMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator))),
        };
    }

    private static List<string> SplitOf(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    /// <summary>
    /// 解析逗号分隔的枚举名。
    /// </summary>
    /// <returns>枚举值；有一个认不出即为空（交给不支持的筛选报错）。</returns>
    private static List<TEnum>? EnumsOf<TEnum>(string value) where TEnum : struct, Enum
    {
        var parts = SplitOf(value);
        var parsed = new List<TEnum>(parts.Count);
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
