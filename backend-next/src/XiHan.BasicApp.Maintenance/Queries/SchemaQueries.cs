// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Migrations;
using XiHan.BasicApp.Maintenance.Operations.Versions;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Queries;

/// <summary>
/// 版本台账与升级执行记录的查询。
/// </summary>
/// <remarks>
/// 读平台库（台账与执行记录都在默认连接上）；租户库的台账随库隔离落地后接进来。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
public sealed class SchemaQueries(SqlSugarUnitOfWork unitOfWork)
{
    /// <summary>
    /// 分页列出版本台账。
    /// </summary>
    public async Task<Result<PageResult<SchemaVersionResponse>>> ListVersionsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = unitOfWork.ConnectionFor(typeof(SysVersion)).Queryable<SysVersion>();
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(v => v.AppVersion.Contains(keyword) || (v.DbVersion != null && v.DbVersion.Contains(keyword)) || (v.UpgradeNode != null && v.UpgradeNode.Contains(keyword)));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            ISugarQueryable<SysVersion>? filtered = (filter.Field, filter.Operator) switch
            {
                ("isUpgrading", "eq") when bool.TryParse(value, out var upgrading) => query.Where(v => v.IsUpgrading == upgrading),
                ("dbVersion", "eq") => query.Where(v => v.DbVersion == value),
                ("appVersion", "eq") => query.Where(v => v.AppVersion == value),
                _ => null,
            };
            if (filtered is null)
            {
                return FilterUnsupported(filter);
            }

            query = filtered;
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            ISugarQueryable<SysVersion>? sorted = sort.Field switch
            {
                "appVersion" => query.OrderBy(v => v.AppVersion, direction),
                "dbVersion" => query.OrderBy(v => v.DbVersion, direction),
                "upgradedAt" => query.OrderBy(v => v.UpgradedAt, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return SortUnsupported(sort);
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<SchemaVersionResponse>([.. rows.Select(SchemaVersionResponse.From)], total.Value, request.Page);
    }

    /// <summary>
    /// 分页列出升级脚本的执行记录。
    /// </summary>
    /// <remarks>
    /// 缺省按执行时间倒序、同一时间按脚本名。
    /// </remarks>
    public async Task<Result<PageResult<MigrationResponse>>> ListMigrationsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var query = unitOfWork.ConnectionFor(typeof(SysMigrationHistory)).Queryable<SysMigrationHistory>();
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(m => m.Version.Contains(keyword) || m.ScriptName.Contains(keyword) || (m.NodeName != null && m.NodeName.Contains(keyword)));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            ISugarQueryable<SysMigrationHistory>? filtered = (filter.Field, filter.Operator) switch
            {
                ("version", "eq") => query.Where(m => m.Version == value),
                ("version", "contains") => query.Where(m => m.Version.Contains(value)),
                ("scriptName", "contains") => query.Where(m => m.ScriptName.Contains(value)),
                ("nodeName", "contains") => query.Where(m => m.NodeName != null && m.NodeName.Contains(value)),
                ("success", "eq") when bool.TryParse(value, out var success) => query.Where(m => m.Success == success),
                ("executedAt", "ge") when TimeOf(value) is { } from => query.Where(m => m.ExecutedAt >= from),
                ("executedAt", "lt") when TimeOf(value) is { } to => query.Where(m => m.ExecutedAt < to),
                _ => null,
            };
            if (filtered is null)
            {
                return FilterUnsupported(filter);
            }

            query = filtered;
        }

        if (request.Sorts.Count == 0)
        {
            query = query.OrderBy(m => m.ExecutedAt, OrderByType.Desc).OrderBy(m => m.ScriptName);
        }

        foreach (var sort in request.Sorts)
        {
            var direction = sort.Descending ? OrderByType.Desc : OrderByType.Asc;
            ISugarQueryable<SysMigrationHistory>? sorted = sort.Field switch
            {
                "executedAt" => query.OrderBy(m => m.ExecutedAt, direction),
                "version" => query.OrderBy(m => m.Version, direction),
                "scriptName" => query.OrderBy(m => m.ScriptName, direction),
                "durationMs" => query.OrderBy(m => m.DurationMs, direction),
                _ => null,
            };
            if (sorted is null)
            {
                return SortUnsupported(sort);
            }

            query = sorted;
        }

        var total = new RefAsync<int>();
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, context.CancellationToken).ConfigureAwait(false);
        return new PageResult<MigrationResponse>([.. rows.Select(MigrationResponse.From)], total.Value, request.Page);
    }

    /// <summary>
    /// 取一条版本台账。
    /// </summary>
    public async Task<Result<SchemaVersionResponse>> GetVersionAsync(Guid id, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var row = await unitOfWork.ConnectionFor(typeof(SysVersion)).Queryable<SysVersion>().FirstAsync(v => v.Id == id, context.CancellationToken).ConfigureAwait(false);
        return row is null ? NotFound(id) : SchemaVersionResponse.From(row);
    }

    /// <summary>
    /// 取一条执行记录。
    /// </summary>
    public async Task<Result<MigrationResponse>> GetMigrationAsync(Guid id, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var row = await unitOfWork.ConnectionFor(typeof(SysMigrationHistory)).Queryable<SysMigrationHistory>().FirstAsync(m => m.Id == id, context.CancellationToken).ConfigureAwait(false);
        return row is null ? NotFound(id) : MigrationResponse.From(row);
    }

    private static Error NotFound(Guid id)
        => Error.NotFound(MaintenanceErrors.SchemaRecordNotFound, MaintenanceMessages.SchemaRecordNotFound, MessageArguments.Of(("id", id)));

    private static Error FilterUnsupported(PageFilter filter)
        => Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));

    private static Error SortUnsupported(PageSort sort)
        => Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));

    private static DateTimeOffset? TimeOf(string value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time) ? time : null;
}
