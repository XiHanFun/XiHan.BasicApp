// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.AccessLogs;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 访问日志查询。
/// </summary>
/// <remarks>
/// 关键字匹配操作 ID、路径与错误码；另有筛选 <c>operationId</c>（eq / contains）、<c>method</c>、<c>path</c>（contains）、<c>result</c>（eq / in）、
/// <c>statusCode</c>、<c>errorCode</c>、<c>durationMs</c>（ge / le）；另有排序 <c>durationMs</c>、<c>statusCode</c>、<c>operationId</c>。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class AccessLogQueries(SqlSugarUnitOfWork unitOfWork) : AuditLogQueries<SysAccessLog, AccessLogResponse, AccessLogResponse>(unitOfWork)
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysAccessLog> ApplyKeyword(ISugarQueryable<SysAccessLog> query, string keyword)
        => query.Where(e => e.OperationId.Contains(keyword) || (e.Path != null && e.Path.Contains(keyword)) || (e.ErrorCode != null && e.ErrorCode.Contains(keyword)));

    /// <inheritdoc />
    protected override Result<ISugarQueryable<SysAccessLog>> ApplyFilter(ISugarQueryable<SysAccessLog> query, string field, string op, string value)
        => (field, op) switch
        {
            ("operationId", "eq") => Result.Success(query.Where(e => e.OperationId == value)),
            ("operationId", "contains") => Result.Success(query.Where(e => e.OperationId.Contains(value))),
            ("method", "eq") => Result.Success(query.Where(e => e.Method == value)),
            ("path", "contains") => Result.Success(query.Where(e => e.Path!.Contains(value))),
            ("result", "eq") => TryParseEnum<AccessResult>(value, out var result) ? Result.Success(query.Where(e => e.Result == result)) : InvalidValue(field, value),
            ("result", "in") => ParseEnums<AccessResult>(value) is { } results ? Result.Success(query.Where(e => results.Contains(e.Result))) : InvalidValue(field, value),
            ("statusCode", "eq") => int.TryParse(value, out var status) ? Result.Success(query.Where(e => e.StatusCode == status)) : InvalidValue(field, value),
            ("errorCode", "eq") => Result.Success(query.Where(e => e.ErrorCode == value)),
            ("durationMs", "ge") => long.TryParse(value, out var min) ? Result.Success(query.Where(e => e.DurationMs >= min)) : InvalidValue(field, value),
            ("durationMs", "le") => long.TryParse(value, out var max) ? Result.Success(query.Where(e => e.DurationMs <= max)) : InvalidValue(field, value),
            _ => Unsupported(),
        };

    /// <inheritdoc />
    protected override ISugarQueryable<SysAccessLog>? ApplySort(ISugarQueryable<SysAccessLog> query, string field, OrderByType direction)
        => field switch
        {
            "durationMs" => query.OrderBy(e => e.DurationMs, direction),
            "statusCode" => query.OrderBy(e => e.StatusCode, direction),
            "operationId" => query.OrderBy(e => e.OperationId, direction),
            _ => null,
        };

    /// <inheritdoc />
    protected override AccessLogResponse MapItem(SysAccessLog entity) => AccessLogResponse.From(entity);

    /// <inheritdoc />
    protected override AccessLogResponse MapDetail(SysAccessLog entity) => AccessLogResponse.From(entity);
}
