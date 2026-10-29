// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.OperationLogs;
using XiHan.BasicApp.Auditing.Operations.Traces;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 链路聚合查询。
/// </summary>
/// <remarks>
/// 按维度在当前一侧的七张分表日志里各取至多「每类上限 + 1」条（多取一条判断截断），再取操作日志——维度是 trace-id 或用户时直接按列查，
/// 其余维度（登录名、会话、客户端地址）操作日志没有对应的列，就按其他日志里出现过的 trace-id 去取。合并后按发生时间正序。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
/// <param name="operations">操作日志查询。</param>
public sealed class TraceQueries(SqlSugarUnitOfWork unitOfWork, OperationLogQueries operations)
{
    /// <summary>
    /// 按 trace-id 反查操作日志时最多带多少个 trace-id。
    /// </summary>
    private const int MaxTraceIds = 500;

    private static readonly DateTime _earliest = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// 取时间线。
    /// </summary>
    public async ValueTask<Result<TraceTimelineResponse>> TimelineAsync(TraceTimelineRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var value = request.Value.Trim();
        if (value.Length == 0)
        {
            return Error.Validation(AuditingErrors.TraceQueryInvalid, AuditingMessages.TraceValueRequired);
        }

        if (request.From is { } from && request.To is { } to && from > to)
        {
            return Error.Validation(AuditingErrors.TraceQueryInvalid, AuditingMessages.TraceRangeInvalid);
        }

        Guid? id = null;
        if (request.Dimension is TraceDimension.UserId or TraceDimension.SessionId)
        {
            if (!Guid.TryParse(value, out var parsed))
            {
                return Error.Validation(AuditingErrors.FilterValueInvalid, AuditingMessages.FilterValueInvalid, MessageArguments.Of(("field", "value"), ("value", value)));
            }

            id = parsed;
        }

        var wanted = request.LogTypes is { Count: > 0 } types ? types.ToHashSet() : Enum.GetValues<TraceLogType>().ToHashSet();
        var take = request.MaxPerType;
        var items = new List<TraceTimelineItem>();
        var counts = new List<TraceTypeCount>();
        var truncated = false;
        var traces = new HashSet<string>(StringComparer.Ordinal);

        async Task CollectAsync<TEntity>(TraceLogType type, Func<TEntity, TraceTimelineItem> map)
            where TEntity : AuditLogRecord, new()
        {
            // 操作日志要靠其他日志里的 trace-id 反查时，其他类型即使没被选中也要取来收集 trace-id
            var needed = wanted.Contains(type);
            if (!needed && !(wanted.Contains(TraceLogType.Operation) && NeedsTraceLookup(request.Dimension)))
            {
                return;
            }

            var rows = await FetchAsync<TEntity>(request, value, id, take + 1, context).ConfigureAwait(false);
            foreach (var row in rows)
            {
                if (traces.Count < MaxTraceIds && row.TraceId.Length > 0)
                {
                    traces.Add(row.TraceId);
                }
            }

            if (!needed)
            {
                return;
            }

            truncated |= rows.Count > take;
            var kept = rows.Take(take).Select(map).ToList();
            items.AddRange(kept);
            counts.Add(new TraceTypeCount(type, kept.Count));
        }

        await CollectAsync<SysAccessLog>(TraceLogType.Access, AccessItem).ConfigureAwait(false);
        await CollectAsync<SysOpenApiLog>(TraceLogType.OpenApi, OpenApiItem).ConfigureAwait(false);
        await CollectAsync<SysLoginLog>(TraceLogType.Login, LoginItem).ConfigureAwait(false);
        await CollectAsync<SysExceptionLog>(TraceLogType.Exception, ExceptionItem).ConfigureAwait(false);
        await CollectAsync<SysDiffLog>(TraceLogType.Diff, DiffItem).ConfigureAwait(false);
        await CollectAsync<SysPermissionChangeLog>(TraceLogType.PermissionChange, PermissionChangeItem).ConfigureAwait(false);
        await CollectAsync<SysConditionHit>(TraceLogType.ConditionHit, ConditionHitItem).ConfigureAwait(false);

        if (wanted.Contains(TraceLogType.Operation))
        {
            var rows = request.Dimension switch
            {
                TraceDimension.TraceId => await operations.ListByTracesAsync([value], take + 1, context).ConfigureAwait(false),
                TraceDimension.UserId => await operations.ListByUserAsync(id!.Value, request.From, request.To, take + 1, context).ConfigureAwait(false),
                _ => await operations.ListByTracesAsync(traces, take + 1, context).ConfigureAwait(false),
            };
            truncated |= rows.Count > take;
            var kept = rows.Take(take).Where(r => Within(r.OccurredAt, request)).Select(OperationItem).ToList();
            items.AddRange(kept);
            counts.Add(new TraceTypeCount(TraceLogType.Operation, kept.Count));
        }

        var ordered = items.OrderBy(static i => i.OccurredAt).ThenBy(static i => i.LogType).ToList();
        return new TraceTimelineResponse(ordered, counts.OrderBy(static c => c.LogType).ToList(), truncated, ordered.Count);
    }

    private static bool NeedsTraceLookup(TraceDimension dimension) => dimension is not (TraceDimension.TraceId or TraceDimension.UserId);

    private static bool Within(DateTimeOffset at, TraceTimelineRequest request)
        => (request.From is not { } from || at >= from) && (request.To is not { } to || at <= to);

    private async Task<List<TEntity>> FetchAsync<TEntity>(TraceTimelineRequest request, string value, Guid? id, int take, InvocationContext context)
        where TEntity : AuditLogRecord, new()
    {
        var tenant = context.GetSecurity().Tenant?.Id ?? TenantIds.Platform;
        var query = unitOfWork.ConnectionFor(typeof(TEntity)).Queryable<TEntity>().Where(e => e.TenantId == tenant).Where(DimensionOf<TEntity>(request.Dimension, value, id));
        DateTime? from = request.From?.UtcDateTime;
        DateTime? to = request.To?.UtcDateTime;
        if (from is { } begin)
        {
            query = query.Where(e => e.OccurredAt >= begin);
        }

        if (to is { } end)
        {
            query = query.Where(e => e.OccurredAt <= end);
        }

        query = from is null && to is null ? query.SplitTable() : query.SplitTable(from ?? _earliest, to ?? DateTime.UtcNow.AddYears(1));
        return await query.OrderBy(e => e.OccurredAt, OrderByType.Asc).Take(take).ToListAsync(context.CancellationToken).ConfigureAwait(false);
    }

    private static Expression<Func<TEntity, bool>> DimensionOf<TEntity>(TraceDimension dimension, string value, Guid? id)
        where TEntity : AuditLogRecord
        => dimension switch
        {
            TraceDimension.TraceId => e => e.TraceId == value,
            TraceDimension.UserId => e => e.UserId == id,
            TraceDimension.UserName => e => e.UserName == value,
            TraceDimension.SessionId => e => e.SessionId == id,
            _ => e => e.ClientIp == value,
        };

    private static DateTimeOffset At(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static TraceTimelineItem Item(TraceLogType type, AuditLogRecord log, string title, string? summary, TraceItemStatus status, string? method = null, string? path = null, int? statusCode = null, long? durationMs = null)
        => new(type, log.Id, At(log.OccurredAt), title, summary, status, log.TraceId, log.UserId, log.UserName, log.SessionId, log.ClientIp, method, path, statusCode, durationMs);

    private static TraceTimelineItem AccessItem(SysAccessLog log)
        => Item(TraceLogType.Access, log, log.OperationId, log.ErrorCode ?? log.Path, log.Result switch
        {
            AccessResult.Success => TraceItemStatus.Success,
            AccessResult.ServerError => TraceItemStatus.Failure,
            _ => TraceItemStatus.Warning,
        }, log.Method, log.Path, log.StatusCode, log.DurationMs);

    private static TraceTimelineItem OpenApiItem(SysOpenApiLog log)
        => Item(TraceLogType.OpenApi, log, log.Path, log.ErrorCode ?? log.ClientId, log.Succeeded ? TraceItemStatus.Success : TraceItemStatus.Failure, log.Method, log.Path, log.StatusCode, log.DurationMs);

    private static TraceTimelineItem LoginItem(SysLoginLog log)
        => Item(TraceLogType.Login, log, log.Event.ToString(), log.ErrorCode ?? log.Detail, log.Succeeded ? TraceItemStatus.Success : TraceItemStatus.Warning);

    private static TraceTimelineItem ExceptionItem(SysExceptionLog log)
        => Item(TraceLogType.Exception, log, log.ExceptionType, log.Message.Length > 256 ? log.Message[..256] : log.Message, TraceItemStatus.Failure, log.Method, log.Path);

    private static TraceTimelineItem DiffItem(SysDiffLog log)
        => Item(TraceLogType.Diff, log, log.EntityType, log.EntityId, TraceItemStatus.Info);

    private static TraceTimelineItem PermissionChangeItem(SysPermissionChangeLog log)
        => Item(TraceLogType.PermissionChange, log, log.ChangeType.ToString(), log.RoleName ?? log.TargetUserName ?? log.EditionCode, TraceItemStatus.Info);

    private static TraceTimelineItem ConditionHitItem(SysConditionHit log)
        => Item(TraceLogType.ConditionHit, log, log.PolicyCode, log.PolicyName, log.IsBlocked ? TraceItemStatus.Warning : TraceItemStatus.Info, log.Method, log.Path);

    private static TraceTimelineItem OperationItem(OperationLogResponse log)
        => new(TraceLogType.Operation, log.Id, log.OccurredAt, log.OperationId, log.ErrorCode ?? log.Resource, log.Succeeded ? TraceItemStatus.Success : TraceItemStatus.Failure, log.TraceId, log.UserId, log.UserName, log.SessionId, null, null, null, null, log.DurationMs);
}
