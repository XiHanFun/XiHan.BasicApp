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
/// 执行记录读平台库（默认连接）；版本台账另读已就绪的租户独立库（台账在各自的库里，经 <see cref="TenantSchemas"/> 绑定分片）。
/// </remarks>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="tenantSchemas">租户独立库的结构。</param>
public sealed class SchemaQueries(SqlSugarUnitOfWork unitOfWork, TenantSchemas tenantSchemas)
{
    /// <summary>
    /// 分页列出版本台账。
    /// </summary>
    /// <remarks>
    /// 平台库加每个已就绪的独立库一行（库不多，合起来在内存里筛选、排序与分页）；读不到台账的库那一行带原因。
    /// </remarks>
    public async Task<Result<PageResult<SchemaVersionResponse>>> ListVersionsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;
        var rows = new List<SchemaVersionResponse>();
        rows.AddRange((await unitOfWork.ConnectionFor(typeof(SysVersion)).Queryable<SysVersion>().ToListAsync(ct).ConfigureAwait(false)).Select(static r => SchemaVersionResponse.From(r)));
        foreach (var tenantId in await tenantSchemas.ReadyTenantsAsync(ct).ConfigureAwait(false))
        {
            rows.Add(await TenantVersionAsync(tenantId, ct).ConfigureAwait(false));
        }

        IEnumerable<SchemaVersionResponse> query = rows;
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(v => Has(v.Database, keyword) || Has(v.AppVersion, keyword) || Has(v.DbVersion, keyword) || Has(v.UpgradeNode, keyword));
        }

        foreach (var filter in request.Filters)
        {
            var value = filter.Value ?? string.Empty;
            IEnumerable<SchemaVersionResponse>? filtered = (filter.Field, filter.Operator) switch
            {
                ("database", "eq") => query.Where(v => v.Database == value),
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

        // 缺省平台库在前、其余按库名；给了排序即按给的
        var ordered = query.OrderBy(static v => v.Database == SchemaVersionResponse.Platform ? 0 : 1).ThenBy(static v => v.Database, StringComparer.Ordinal);
        if (request.Sorts.Count > 0)
        {
            IOrderedEnumerable<SchemaVersionResponse>? sorted = null;
            foreach (var sort in request.Sorts)
            {
                Func<SchemaVersionResponse, object?>? key = sort.Field switch
                {
                    "database" => static v => v.Database,
                    "appVersion" => static v => v.AppVersion,
                    "dbVersion" => static v => v.DbVersion,
                    "upgradedAt" => static v => v.UpgradedAt,
                    _ => null,
                };
                if (key is null)
                {
                    return SortUnsupported(sort);
                }

                sorted = sorted is null
                    ? sort.Descending ? query.OrderByDescending(key) : query.OrderBy(key)
                    : sort.Descending ? sorted.ThenByDescending(key) : sorted.ThenBy(key);
            }

            ordered = sorted!;
        }

        var all = ordered.ToList();
        var page = all.Skip((request.Page.Index - 1) * request.Page.Size).Take(request.Page.Size).ToList();
        return new PageResult<SchemaVersionResponse>(page, all.Count, request.Page);
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
    /// 取一个库的版本台账。
    /// </summary>
    /// <remarks>
    /// 库名是 <c>platform</c> 或已就绪独立库的租户 ID，别的即找不到；独立库读不到时那一行带原因。
    /// </remarks>
    public async Task<Result<SchemaVersionResponse>> GetVersionAsync(string database, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;
        if (string.Equals(database, SchemaVersionResponse.Platform, StringComparison.Ordinal))
        {
            var row = await unitOfWork.ConnectionFor(typeof(SysVersion)).Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, ct).ConfigureAwait(false);
            return row is null ? NotFound(database) : SchemaVersionResponse.From(row);
        }

        return (await tenantSchemas.ReadyTenantsAsync(ct).ConfigureAwait(false)).Contains(database, StringComparer.Ordinal)
            ? await TenantVersionAsync(database, ct).ConfigureAwait(false)
            : NotFound(database);
    }

    /// <summary>
    /// 读一个独立库的台账，读不到即带上原因。
    /// </summary>
    private async Task<SchemaVersionResponse> TenantVersionAsync(string tenantId, CancellationToken cancellationToken)
    {
        var read = await tenantSchemas.ReadVersionAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!read.TryGetValue(out var found))
        {
            return SchemaVersionResponse.Unreadable(tenantId, read.Error!.Message);
        }

        return found.TryGetValue(out var row)
            ? SchemaVersionResponse.From(row, tenantId)
            : SchemaVersionResponse.Unreadable(tenantId, $"The database of tenant '{tenantId}' has no schema version ledger.");
    }

    private static bool Has(string? value, string keyword) => value is not null && value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static Error NotFound(string database)
        => Error.NotFound(MaintenanceErrors.SchemaRecordNotFound, MaintenanceMessages.SchemaRecordNotFound, MessageArguments.Of(("id", database)));

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
