// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.OpenApiLogs;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 开放接口日志查询：关键字匹配路径、客户端标识与应用名称；另有筛选 <c>clientId</c>、<c>method</c>、<c>path</c>（contains）、<c>statusCode</c>、<c>succeeded</c>、
/// <c>signatureValid</c>、<c>durationMs</c>（ge / le）；另有排序 <c>durationMs</c>、<c>statusCode</c>、<c>path</c>。列表项不含正文摘要，详情才有。
/// </summary>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class OpenApiLogQueries(SqlSugarUnitOfWork unitOfWork) : AuditLogQueries<SysOpenApiLog, OpenApiLogResponse, OpenApiLogDetailResponse>(unitOfWork)
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysOpenApiLog> ApplyKeyword(ISugarQueryable<SysOpenApiLog> query, string keyword)
        => query.Where(e => e.Path.Contains(keyword) || (e.ClientId != null && e.ClientId.Contains(keyword)) || (e.AppName != null && e.AppName.Contains(keyword)));

    /// <inheritdoc />
    protected override Result<ISugarQueryable<SysOpenApiLog>> ApplyFilter(ISugarQueryable<SysOpenApiLog> query, string field, string op, string value)
        => (field, op) switch
        {
            ("clientId", "eq") => Result.Success(query.Where(e => e.ClientId == value)),
            ("method", "eq") => Result.Success(query.Where(e => e.Method == value)),
            ("path", "contains") => Result.Success(query.Where(e => e.Path.Contains(value))),
            ("statusCode", "eq") => int.TryParse(value, out var status) ? Result.Success(query.Where(e => e.StatusCode == status)) : InvalidValue(field, value),
            ("succeeded", "eq") => bool.TryParse(value, out var succeeded) ? Result.Success(query.Where(e => e.Succeeded == succeeded)) : InvalidValue(field, value),
            ("signatureValid", "eq") => bool.TryParse(value, out var valid) ? Result.Success(query.Where(e => e.SignatureValid == valid)) : InvalidValue(field, value),
            ("durationMs", "ge") => long.TryParse(value, out var min) ? Result.Success(query.Where(e => e.DurationMs >= min)) : InvalidValue(field, value),
            ("durationMs", "le") => long.TryParse(value, out var max) ? Result.Success(query.Where(e => e.DurationMs <= max)) : InvalidValue(field, value),
            _ => Unsupported(),
        };

    /// <inheritdoc />
    protected override ISugarQueryable<SysOpenApiLog>? ApplySort(ISugarQueryable<SysOpenApiLog> query, string field, OrderByType direction)
        => field switch
        {
            "durationMs" => query.OrderBy(e => e.DurationMs, direction),
            "statusCode" => query.OrderBy(e => e.StatusCode, direction),
            "path" => query.OrderBy(e => e.Path, direction),
            _ => null,
        };

    /// <inheritdoc />
    protected override OpenApiLogResponse MapItem(SysOpenApiLog entity) => OpenApiLogResponse.From(entity);

    /// <inheritdoc />
    protected override OpenApiLogDetailResponse MapDetail(SysOpenApiLog entity) => OpenApiLogDetailResponse.From(entity);
}
