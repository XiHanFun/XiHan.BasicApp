// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.ExceptionLogs;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 异常日志查询。
/// </summary>
/// <remarks>
/// 关键字匹配异常类型、消息与操作 ID；另有筛选 <c>exceptionType</c>（eq / contains）、<c>operationId</c>、<c>path</c>（contains）、<c>machineName</c>、<c>environmentName</c>；
/// 另有排序 <c>exceptionType</c>、<c>operationId</c>。列表项不含堆栈，详情才有。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class ExceptionLogQueries(SqlSugarUnitOfWork unitOfWork) : AuditLogQueries<SysExceptionLog, ExceptionLogResponse, ExceptionLogDetailResponse>(unitOfWork)
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysExceptionLog> ApplyKeyword(ISugarQueryable<SysExceptionLog> query, string keyword)
        => query.Where(e => e.ExceptionType.Contains(keyword) || e.Message.Contains(keyword) || (e.OperationId != null && e.OperationId.Contains(keyword)));

    /// <inheritdoc />
    protected override Result<ISugarQueryable<SysExceptionLog>> ApplyFilter(ISugarQueryable<SysExceptionLog> query, string field, string op, string value)
        => (field, op) switch
        {
            ("exceptionType", "eq") => Result.Success(query.Where(e => e.ExceptionType == value)),
            ("exceptionType", "contains") => Result.Success(query.Where(e => e.ExceptionType.Contains(value))),
            ("operationId", "eq") => Result.Success(query.Where(e => e.OperationId == value)),
            ("path", "contains") => Result.Success(query.Where(e => e.Path!.Contains(value))),
            ("machineName", "eq") => Result.Success(query.Where(e => e.MachineName == value)),
            ("environmentName", "eq") => Result.Success(query.Where(e => e.EnvironmentName == value)),
            _ => Unsupported(),
        };

    /// <inheritdoc />
    protected override ISugarQueryable<SysExceptionLog>? ApplySort(ISugarQueryable<SysExceptionLog> query, string field, OrderByType direction)
        => field switch
        {
            "exceptionType" => query.OrderBy(e => e.ExceptionType, direction),
            "operationId" => query.OrderBy(e => e.OperationId, direction),
            _ => null,
        };

    /// <inheritdoc />
    protected override ExceptionLogResponse MapItem(SysExceptionLog entity) => ExceptionLogResponse.From(entity);

    /// <inheritdoc />
    protected override ExceptionLogDetailResponse MapDetail(SysExceptionLog entity) => ExceptionLogDetailResponse.From(entity);
}
