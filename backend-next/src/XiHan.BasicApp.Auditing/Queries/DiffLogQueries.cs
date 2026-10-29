// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.DiffLogs;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 数据变更日志查询。
/// </summary>
/// <remarks>
/// 关键字匹配实体类型、表名与主键值；另有筛选 <c>kind</c>（eq / in）、<c>entityType</c>（eq / contains）、<c>tableName</c>、<c>entityId</c>、<c>operationId</c>；
/// 另有排序 <c>entityType</c>、<c>kind</c>。列表项不含前后值，详情才有。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class DiffLogQueries(SqlSugarUnitOfWork unitOfWork) : AuditLogQueries<SysDiffLog, DiffLogResponse, DiffLogDetailResponse>(unitOfWork)
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysDiffLog> ApplyKeyword(ISugarQueryable<SysDiffLog> query, string keyword)
        => query.Where(e => e.EntityType.Contains(keyword) || (e.TableName != null && e.TableName.Contains(keyword)) || (e.EntityId != null && e.EntityId.Contains(keyword)));

    /// <inheritdoc />
    protected override Result<ISugarQueryable<SysDiffLog>> ApplyFilter(ISugarQueryable<SysDiffLog> query, string field, string op, string value)
        => (field, op) switch
        {
            ("kind", "eq") => TryParseEnum<DiffKind>(value, out var kind) ? Result.Success(query.Where(e => e.Kind == kind)) : InvalidValue(field, value),
            ("kind", "in") => ParseEnums<DiffKind>(value) is { } kinds ? Result.Success(query.Where(e => kinds.Contains(e.Kind))) : InvalidValue(field, value),
            ("entityType", "eq") => Result.Success(query.Where(e => e.EntityType == value)),
            ("entityType", "contains") => Result.Success(query.Where(e => e.EntityType.Contains(value))),
            ("tableName", "eq") => Result.Success(query.Where(e => e.TableName == value)),
            ("entityId", "eq") => Result.Success(query.Where(e => e.EntityId == value)),
            ("operationId", "eq") => Result.Success(query.Where(e => e.OperationId == value)),
            _ => Unsupported(),
        };

    /// <inheritdoc />
    protected override ISugarQueryable<SysDiffLog>? ApplySort(ISugarQueryable<SysDiffLog> query, string field, OrderByType direction)
        => field switch
        {
            "entityType" => query.OrderBy(e => e.EntityType, direction),
            "kind" => query.OrderBy(e => e.Kind, direction),
            _ => null,
        };

    /// <inheritdoc />
    protected override DiffLogResponse MapItem(SysDiffLog entity) => DiffLogResponse.From(entity);

    /// <inheritdoc />
    protected override DiffLogDetailResponse MapDetail(SysDiffLog entity) => DiffLogDetailResponse.From(entity);
}
