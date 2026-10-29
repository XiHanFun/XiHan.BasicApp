// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.LoginLogs;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 登录日志查询。
/// </summary>
/// <remarks>
/// 关键字匹配登录名、错误码与客户端地址；另有筛选 <c>event</c>（eq / in）、<c>succeeded</c>、<c>errorCode</c>、<c>accountTenantId</c>、<c>impersonatorUserId</c>；
/// 另有排序 <c>event</c>。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class LoginLogQueries(SqlSugarUnitOfWork unitOfWork) : AuditLogQueries<SysLoginLog, LoginLogResponse, LoginLogResponse>(unitOfWork)
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysLoginLog> ApplyKeyword(ISugarQueryable<SysLoginLog> query, string keyword)
        => query.Where(e => (e.UserName != null && e.UserName.Contains(keyword)) || (e.ErrorCode != null && e.ErrorCode.Contains(keyword)) || (e.ClientIp != null && e.ClientIp.Contains(keyword)));

    /// <inheritdoc />
    protected override Result<ISugarQueryable<SysLoginLog>> ApplyFilter(ISugarQueryable<SysLoginLog> query, string field, string op, string value)
        => (field, op) switch
        {
            ("event", "eq") => TryParseEnum<LoginEvent>(value, out var loginEvent) ? Result.Success(query.Where(e => e.Event == loginEvent)) : InvalidValue(field, value),
            ("event", "in") => ParseEnums<LoginEvent>(value) is { } events ? Result.Success(query.Where(e => events.Contains(e.Event))) : InvalidValue(field, value),
            ("succeeded", "eq") => bool.TryParse(value, out var succeeded) ? Result.Success(query.Where(e => e.Succeeded == succeeded)) : InvalidValue(field, value),
            ("errorCode", "eq") => Result.Success(query.Where(e => e.ErrorCode == value)),
            ("accountTenantId", "eq") => Result.Success(query.Where(e => e.AccountTenantId == value)),
            ("impersonatorUserId", "eq") => Guid.TryParse(value, out var impersonator) ? Result.Success(query.Where(e => e.ImpersonatorUserId == impersonator)) : InvalidValue(field, value),
            _ => Unsupported(),
        };

    /// <inheritdoc />
    protected override ISugarQueryable<SysLoginLog>? ApplySort(ISugarQueryable<SysLoginLog> query, string field, OrderByType direction)
        => field switch
        {
            "event" => query.OrderBy(e => e.Event, direction),
            _ => null,
        };

    /// <inheritdoc />
    protected override LoginLogResponse MapItem(SysLoginLog entity) => LoginLogResponse.From(entity);

    /// <inheritdoc />
    protected override LoginLogResponse MapDetail(SysLoginLog entity) => LoginLogResponse.From(entity);
}
