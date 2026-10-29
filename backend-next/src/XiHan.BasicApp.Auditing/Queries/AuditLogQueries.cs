// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 分表日志查询的公共部分（SqlSugar 直查）。
/// </summary>
/// <remarks>
/// 日志严格按上下文隔离——租户主体只看本租户的行，平台主体只看平台一侧（租户列 <c>platform</c>）的行。
/// 公共筛选：<c>occurredAt</c>（<c>ge</c> / <c>gt</c> / <c>le</c> / <c>lt</c>，ISO 8601，同时限定扫描的月表）、<c>traceId</c>、<c>userId</c>、<c>userName</c>、<c>sessionId</c>、<c>clientIp</c>；
/// 公共排序：<c>occurredAt</c>（缺省倒序）、<c>userName</c>。没有时间范围即扫全部月表（ADR-0023）。
/// </remarks>
/// <typeparam name="TEntity">日志实体。</typeparam>
/// <typeparam name="TItem">列表项。</typeparam>
/// <typeparam name="TDetail">详情。</typeparam>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public abstract class AuditLogQueries<TEntity, TItem, TDetail>(SqlSugarUnitOfWork unitOfWork)
    where TEntity : AuditLogRecord, new()
{
    /// <summary>
    /// 发生时间。
    /// </summary>
    protected const string FieldOccurredAt = "occurredAt";

    private const string FieldTraceId = "traceId";
    private const string FieldUserId = "userId";
    private const string FieldUserName = "userName";
    private const string FieldSessionId = "sessionId";
    private const string FieldClientIp = "clientIp";

    /// <summary>
    /// 时间下界没给时的扫描起点（早于任何月表）。
    /// </summary>
    private static readonly DateTime _earliest = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 分页列出日志。
    /// </summary>
    public async ValueTask<Result<PageResult<TItem>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = Scope(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            query = ApplyKeyword(query, request.Keyword.Trim());
        }

        DateTime? from = null;
        DateTime? to = null;
        foreach (var filter in request.Filters)
        {
            var value = filter.Value?.Trim() ?? string.Empty;
            if (string.Equals(filter.Field, FieldOccurredAt, StringComparison.Ordinal))
            {
                if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
                {
                    return Invalid(filter);
                }

                var utc = at.UtcDateTime;
                switch (filter.Operator)
                {
                    case "ge":
                        query = query.Where(e => e.OccurredAt >= utc);
                        from = Later(from, utc);
                        break;
                    case "gt":
                        query = query.Where(e => e.OccurredAt > utc);
                        from = Later(from, utc);
                        break;
                    case "le":
                        query = query.Where(e => e.OccurredAt <= utc);
                        to = Earlier(to, utc);
                        break;
                    case "lt":
                        query = query.Where(e => e.OccurredAt < utc);
                        to = Earlier(to, utc);
                        break;
                    default:
                        return Unsupported(filter);
                }

                continue;
            }

            var filtered = (filter.Field, filter.Operator) switch
            {
                (FieldTraceId, "eq") => Result.Success(query.Where(e => e.TraceId == value)),
                (FieldUserId, "eq") => Guid.TryParse(value, out var userId) ? Result.Success(query.Where(e => e.UserId == userId)) : Invalid(filter),
                (FieldUserName, "eq") => Result.Success(query.Where(e => e.UserName == value)),
                (FieldUserName, "contains") => Result.Success(query.Where(e => e.UserName!.Contains(value))),
                (FieldSessionId, "eq") => Guid.TryParse(value, out var sessionId) ? Result.Success(query.Where(e => e.SessionId == sessionId)) : Invalid(filter),
                (FieldClientIp, "eq") => Result.Success(query.Where(e => e.ClientIp == value)),
                (FieldClientIp, "contains") => Result.Success(query.Where(e => e.ClientIp!.Contains(value))),
                _ => ApplyFilter(query, filter.Field, filter.Operator, value),
            };
            if (!filtered.TryGetValue(out query))
            {
                return filtered.Error!.Code == AuditingErrors.QueryUnsupported ? Unsupported(filter) : filtered.Error!;
            }
        }

        if (from is { } begin && to is { } end && begin > end)
        {
            return new PageResult<TItem>([], 0, request.Page);
        }

        // 条件都在分表之前施加（SqlSugar 下推到每张月表再 UNION ALL），排序与分页在之后
        query = from is null && to is null ? query.SplitTable() : query.SplitTable(from ?? _earliest, to ?? DateTime.UtcNow.AddYears(1));
        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(e => e.OccurredAt, OrderByType.Desc).OrderBy(e => e.Id, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldOccurredAt => query.OrderBy(e => e.OccurredAt, direction),
                FieldUserName => query.OrderBy(e => e.UserName, direction),
                _ => ApplySort(query, sort.Field, direction),
            };
            if (sorted is null)
            {
                return Error.Validation(AuditingErrors.QueryUnsupported, AuditingMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<TItem>(rows.Select(MapItem).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 按主键取一条（扫全部月表）。
    /// </summary>
    /// <remarks>
    /// 不在当前一侧即找不到。
    /// </remarks>
    public async ValueTask<Result<TDetail>> GetAsync(Guid id, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var rows = await Scope(context).Where(e => e.Id == id).SplitTable().Take(1).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return rows.Count == 0
            ? Error.NotFound(AuditingErrors.LogNotFound, AuditingMessages.LogNotFound, MessageArguments.Of(("id", id)))
            : MapDetail(rows[0]);
    }

    /// <summary>
    /// 当前一侧的日志：租户主体是本租户，平台主体是 <c>platform</c>。
    /// </summary>
    protected ISugarQueryable<TEntity> Scope(InvocationContext context)
    {
        var tenant = context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
        return unitOfWork.ConnectionFor(typeof(TEntity)).Queryable<TEntity>().Where(e => e.TenantId == tenant);
    }

    /// <summary>
    /// 关键字匹配哪些列。
    /// </summary>
    protected abstract ISugarQueryable<TEntity> ApplyKeyword(ISugarQueryable<TEntity> query, string keyword);

    /// <summary>
    /// 本类日志特有的筛选。
    /// </summary>
    /// <returns>筛选后的查询；不认识的返回 <see cref="Unsupported()"/>。</returns>
    protected abstract Result<ISugarQueryable<TEntity>> ApplyFilter(ISugarQueryable<TEntity> query, string field, string op, string value);

    /// <summary>
    /// 本类日志特有的排序。
    /// </summary>
    /// <returns>排序后的查询；不认识的返回 <see langword="null"/>。</returns>
    protected abstract ISugarQueryable<TEntity>? ApplySort(ISugarQueryable<TEntity> query, string field, OrderByType direction);

    /// <summary>
    /// 投影成列表项。
    /// </summary>
    protected abstract TItem MapItem(TEntity entity);

    /// <summary>
    /// 投影成详情。
    /// </summary>
    protected abstract TDetail MapDetail(TEntity entity);

    /// <summary>
    /// 不支持的筛选（由 <see cref="ListAsync"/> 补上字段与运算符）。
    /// </summary>
    protected static Error Unsupported() => Error.Validation(AuditingErrors.QueryUnsupported, AuditingMessages.FilterUnsupported);

    /// <summary>
    /// 枚举值按名字解析（不区分大小写）。
    /// </summary>
    protected static bool TryParseEnum<TEnum>(string value, out TEnum parsed)
        where TEnum : struct, Enum
        => Enum.TryParse(value, ignoreCase: true, out parsed) && Enum.IsDefined(parsed);

    /// <summary>
    /// 不合法的筛选值。
    /// </summary>
    protected static Error InvalidValue(string field, string value)
        => Error.Validation(AuditingErrors.FilterValueInvalid, AuditingMessages.FilterValueInvalid, MessageArguments.Of(("field", field), ("value", value)));

    /// <summary>
    /// 解析 <c>in</c> 筛选的值。
    /// </summary>
    /// <param name="value">逗号分隔的枚举名。</param>
    /// <returns>解析出的枚举；有一个不认识即 <see langword="null"/>。</returns>
    protected static TEnum[]? ParseEnums<TEnum>(string value)
        where TEnum : struct, Enum
    {
        var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parsed = new TEnum[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (!TryParseEnum(parts[i], out parsed[i]))
            {
                return null;
            }
        }

        return parsed.Length == 0 ? null : parsed;
    }

    private static Error Invalid(PageFilter filter) => InvalidValue(filter.Field, filter.Value ?? string.Empty);

    private static Error Unsupported(PageFilter filter)
        => Error.Validation(AuditingErrors.QueryUnsupported, AuditingMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));

    private static DateTime Later(DateTime? current, DateTime value) => current is { } c && c > value ? c : value;

    private static DateTime Earlier(DateTime? current, DateTime value) => current is { } c && c < value ? c : value;
}
