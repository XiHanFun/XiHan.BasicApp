// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Operations.PermissionChangeLogs;
using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Auditing.Queries;

/// <summary>
/// 权限变更日志查询。
/// </summary>
/// <remarks>
/// 关键字匹配角色名、目标用户、套餐编码与权限码；另有筛选 <c>changeType</c>（eq / in）、<c>roleId</c>、<c>targetUserId</c>、<c>targetUserName</c>（eq / contains）、
/// <c>editionId</c>、<c>targetTenantId</c>、<c>permissionCodes</c>（contains）；另有排序 <c>changeType</c>、<c>roleName</c>。
/// </remarks>
/// <param name="unitOfWork">SqlSugar 工作单元。</param>
public sealed class PermissionChangeLogQueries(SqlSugarUnitOfWork unitOfWork) : AuditLogQueries<SysPermissionChangeLog, PermissionChangeLogResponse, PermissionChangeLogResponse>(unitOfWork)
{
    /// <inheritdoc />
    protected override ISugarQueryable<SysPermissionChangeLog> ApplyKeyword(ISugarQueryable<SysPermissionChangeLog> query, string keyword)
        => query.Where(e => (e.RoleName != null && e.RoleName.Contains(keyword)) || (e.TargetUserName != null && e.TargetUserName.Contains(keyword))
            || (e.EditionCode != null && e.EditionCode.Contains(keyword)) || (e.PermissionCodes != null && e.PermissionCodes.Contains(keyword)));

    /// <inheritdoc />
    protected override Result<ISugarQueryable<SysPermissionChangeLog>> ApplyFilter(ISugarQueryable<SysPermissionChangeLog> query, string field, string op, string value)
        => (field, op) switch
        {
            ("changeType", "eq") => TryParseEnum<PermissionChangeType>(value, out var type) ? Result.Success(query.Where(e => e.ChangeType == type)) : InvalidValue(field, value),
            ("changeType", "in") => ParseEnums<PermissionChangeType>(value) is { } types ? Result.Success(query.Where(e => types.Contains(e.ChangeType))) : InvalidValue(field, value),
            ("roleId", "eq") => Guid.TryParse(value, out var roleId) ? Result.Success(query.Where(e => e.RoleId == roleId)) : InvalidValue(field, value),
            ("targetUserId", "eq") => Guid.TryParse(value, out var userId) ? Result.Success(query.Where(e => e.TargetUserId == userId)) : InvalidValue(field, value),
            ("targetUserName", "eq") => Result.Success(query.Where(e => e.TargetUserName == value)),
            ("targetUserName", "contains") => Result.Success(query.Where(e => e.TargetUserName!.Contains(value))),
            ("editionId", "eq") => Guid.TryParse(value, out var editionId) ? Result.Success(query.Where(e => e.EditionId == editionId)) : InvalidValue(field, value),
            ("targetTenantId", "eq") => Result.Success(query.Where(e => e.TargetTenantId == value)),
            ("permissionCodes", "contains") => Result.Success(query.Where(e => e.PermissionCodes!.Contains(value))),
            _ => Unsupported(),
        };

    /// <inheritdoc />
    protected override ISugarQueryable<SysPermissionChangeLog>? ApplySort(ISugarQueryable<SysPermissionChangeLog> query, string field, OrderByType direction)
        => field switch
        {
            "changeType" => query.OrderBy(e => e.ChangeType, direction),
            "roleName" => query.OrderBy(e => e.RoleName, direction),
            _ => null,
        };

    /// <inheritdoc />
    protected override PermissionChangeLogResponse MapItem(SysPermissionChangeLog entity) => PermissionChangeLogResponse.From(entity);

    /// <inheritdoc />
    protected override PermissionChangeLogResponse MapDetail(SysPermissionChangeLog entity) => PermissionChangeLogResponse.From(entity);
}
