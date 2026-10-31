// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using XiHan.BasicApp.Auditing.Operations.OperationLogs;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.Framework.Auditing.Database.Trail;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Localization.Abstractions.Messages;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 操作日志查询。
/// </summary>
/// <remarks>
/// SqlSugar 直查框架 <c>Auditing.Database</c> 的 <c>AuditRecord</c> 表，它不分表、不实现 <c>IMultiTenant</c>。
/// 租户主体只看本租户的记录，平台主体只看没有租户的记录。关键字匹配操作 ID、资源与错误码；筛选 <c>occurredAt</c>（ge / gt / le / lt）、<c>operationId</c>（eq / contains）、
/// <c>succeeded</c>、<c>errorCode</c>、<c>traceId</c>、<c>userId</c>；排序 <c>occurredAt</c>（缺省倒序）、<c>operationId</c>。
/// 列表要页码与总数，框架查询契约 <c>IAuditTrail</c> 只有按写入序号的续页，所以管理端列表走表本身（ADR-0028 写明表结构归应用登记）。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class OperationLogQueries(SqlSugarUnitOfWork unitOfWork)
{
    private const string FieldOccurredAt = "occurredAt";
    private const string FieldOperationId = "operationId";

    /// <summary>
    /// 分页列出操作日志。
    /// </summary>
    public async ValueTask<Result<PageResult<OperationLogResponse>>> ListAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = Scope(context);
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(r => r.Action.Contains(keyword) || (r.Resource != null && r.Resource.Contains(keyword)) || (r.ErrorCode != null && r.ErrorCode.Contains(keyword)));
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
            query = query.OrderBy(r => r.Timestamp, OrderByType.Desc).OrderBy(r => r.Sequence, OrderByType.Desc);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            var sorted = sort.Field switch
            {
                FieldOccurredAt => query.OrderBy(r => r.Timestamp, direction),
                FieldOperationId => query.OrderBy(r => r.Action, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return Error.Validation(AuditingErrors.QueryUnsupported, AuditingMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<OperationLogResponse>(rows.Select(r => Item(r, context)).ToList(), total.Value, request.Page);
    }

    /// <summary>
    /// 按主键取一条操作日志。
    /// </summary>
    /// <remarks>
    /// 不在当前一侧即找不到。
    /// </remarks>
    public async ValueTask<Result<OperationLogDetailResponse>> GetAsync(Guid id, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var record = await Scope(context).Where(r => r.Id == id).FirstAsync(context.CancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return Error.NotFound(AuditingErrors.LogNotFound, AuditingMessages.LogNotFound, MessageArguments.Of(("id", id)));
        }

        var item = Item(record, context);
        return new OperationLogDetailResponse(item.Id, item.OccurredAt, item.TraceId, item.UserId, item.UserName, item.SessionId, item.OperationId, item.Description, item.Succeeded, item.ErrorCode, item.ErrorMessage, item.Resource, item.DurationMs, item.ClientIp, item.Browser, item.Os, item.Device, item.Location, DetailText(record.DetailsJson, "userAgent"), record.Sequence, record.DetailsJson);
    }

    /// <summary>
    /// 按 trace-id 取当前一侧的记录（链路聚合用），按写入序号正序。
    /// </summary>
    public async Task<IReadOnlyList<OperationLogResponse>> ListByTracesAsync(IReadOnlyCollection<string> traceIds, int take, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(traceIds);
        ArgumentNullException.ThrowIfNull(context);
        if (traceIds.Count == 0)
        {
            return [];
        }

        var ids = traceIds.ToArray();
        var rows = await Scope(context).Where(r => ids.Contains(r.TraceId)).OrderBy(r => r.Sequence, OrderByType.Asc).Take(take).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return rows.Select(r => Item(r, context)).ToList();
    }

    /// <summary>
    /// 按用户取当前一侧的记录（链路聚合用），按写入序号正序。
    /// </summary>
    public async Task<IReadOnlyList<OperationLogResponse>> ListByUserAsync(Guid userId, DateTimeOffset? from, DateTimeOffset? to, int take, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var principal = userId.ToString("D");
        var query = Scope(context).Where(r => r.PrincipalId == principal);
        if (from is { } begin)
        {
            query = query.Where(r => r.Timestamp >= begin);
        }

        if (to is { } end)
        {
            query = query.Where(r => r.Timestamp <= end);
        }

        var rows = await query.OrderBy(r => r.Sequence, OrderByType.Asc).Take(take).ToListAsync(context.CancellationToken).ConfigureAwait(false);
        return rows.Select(r => Item(r, context)).ToList();
    }

    private ISugarQueryable<AuditRecord> Scope(InvocationContext context)
    {
        var client = unitOfWork.ConnectionFor(typeof(AuditRecord));
        return context.GetSecurity().Tenant?.Id is { } tenant
            ? client.Queryable<AuditRecord>().Where(r => r.TenantId == tenant)
            : client.Queryable<AuditRecord>().Where(r => r.TenantId == null);
    }

    private static Result<ISugarQueryable<AuditRecord>> Apply(ISugarQueryable<AuditRecord> query, PageFilter filter)
    {
        var value = filter.Value?.Trim() ?? string.Empty;
        if (string.Equals(filter.Field, FieldOccurredAt, StringComparison.Ordinal))
        {
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var at))
            {
                return Invalid(filter, value);
            }

            return filter.Operator switch
            {
                "ge" => Result.Success(query.Where(r => r.Timestamp >= at)),
                "gt" => Result.Success(query.Where(r => r.Timestamp > at)),
                "le" => Result.Success(query.Where(r => r.Timestamp <= at)),
                "lt" => Result.Success(query.Where(r => r.Timestamp < at)),
                _ => Unsupported(filter),
            };
        }

        return (filter.Field, filter.Operator) switch
        {
            (FieldOperationId, "eq") => Result.Success(query.Where(r => r.Action == value)),
            (FieldOperationId, "contains") => Result.Success(query.Where(r => r.Action.Contains(value))),
            ("succeeded", "eq") => bool.TryParse(value, out var succeeded) ? Result.Success(query.Where(r => r.Succeeded == succeeded)) : Invalid(filter, value),
            ("errorCode", "eq") => Result.Success(query.Where(r => r.ErrorCode == value)),
            ("traceId", "eq") => Result.Success(query.Where(r => r.TraceId == value)),
            ("userId", "eq") => Guid.TryParse(value, out var userId) ? Result.Success(ByPrincipal(query, userId)) : Invalid(filter, value),
            _ => Unsupported(filter),
        };
    }

    /// <summary>
    /// 把审计记录投影成列表项。
    /// </summary>
    /// <remarks>
    /// 登录名、会话、耗时与写入时记下的客户端信息取细节；描述是操作的本地化标题（与 OpenAPI 摘要同一个键，取不到为空）；
    /// 有消息键的错误按调用方语言重新渲染（错误细节列就是模板参数），字面错误用落库时的文本。
    /// </remarks>
    private static OperationLogResponse Item(AuditRecord record, InvocationContext context)
    {
        string? userName = null;
        Guid? sessionId = null;
        long? durationMs = null;
        string? clientIp = null;
        string? browser = null;
        string? os = null;
        string? device = null;
        string? location = null;
        if (record.DetailsJson is { Length: > 0 } json)
        {
            using var details = JsonDocument.Parse(json);
            var root = details.RootElement;
            userName = Text(root, "userName");
            clientIp = Text(root, "clientIp");
            browser = Text(root, "browser");
            os = Text(root, "os");
            device = Text(root, "device");
            location = Text(root, "location");

            if (root.TryGetProperty("sessionId", out var session) && session.ValueKind == JsonValueKind.String && Guid.TryParse(session.GetString(), out var parsed))
            {
                sessionId = parsed;
            }

            if (root.TryGetProperty("durationMs", out var duration) && duration.TryGetInt64(out var ms))
            {
                durationMs = ms;
            }
        }

        var message = record.Succeeded
            ? null
            : record.ErrorMessageKey is { } key ? context.Localize(key, ArgumentsOf(record.ErrorDetailsJson)) : record.ErrorMessage;
        return new OperationLogResponse(
            record.Id,
            record.Timestamp,
            record.TraceId,
            Guid.TryParse(record.PrincipalId, out var userId) ? userId : null,
            userName,
            sessionId,
            record.Action,
            TitleOf(record.Action, context),
            record.Succeeded,
            record.ErrorCode,
            message,
            record.Resource,
            durationMs,
            clientIp,
            browser,
            os,
            device,
            location);
    }

    /// <summary>
    /// 操作的本地化标题。
    /// </summary>
    /// <remarks>
    /// 键为操作 ID（<c>-</c> 换成 <c>_</c>）加 <c>.title</c>，按调用方的区域取，回退链用尽仍没有即为空。
    /// </remarks>
    private static string? TitleOf(string operationId, InvocationContext context)
    {
        var localizer = context.Services.GetService<IMessageLocalizer>();
        var locale = localizer?.Options.Resolve(context);
        return localizer is not null && locale is not null && localizer.Resolve(operationId.Replace('-', '_') + ".title", locale, null).TryGetValue(out var text) ? text.Text : null;
    }

    private static string? Text(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? DetailText(string? json, string name)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        using var details = JsonDocument.Parse(json);
        return Text(details.RootElement, name);
    }

    private static Dictionary<string, object?>? ArgumentsOf(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            arguments[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.Number => property.Value.TryGetInt64(out var integer) ? integer : property.Value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => property.Value.GetRawText(),
            };
        }

        return arguments;
    }

    private static ISugarQueryable<AuditRecord> ByPrincipal(ISugarQueryable<AuditRecord> query, Guid userId)
    {
        var principal = userId.ToString("D");
        return query.Where(r => r.PrincipalId == principal);
    }

    private static Error Invalid(PageFilter filter, string value)
        => Error.Validation(AuditingErrors.FilterValueInvalid, AuditingMessages.FilterValueInvalid, MessageArguments.Of(("field", filter.Field), ("value", value)));

    private static Error Unsupported(PageFilter filter)
        => Error.Validation(AuditingErrors.QueryUnsupported, AuditingMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));
}
