// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using SqlSugar;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Maintenance.Operations.Migrations;
using XiHan.BasicApp.Maintenance.Operations.Versions;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Maintenance.Queries;

/// <summary>
/// 版本台账与升级执行记录的查询
/// </summary>
/// <remarks>
/// 平台库读默认连接；租户独立库的台账与执行记录在各自的库里，经 <see cref="TenantSchemas"/> 绑定分片读
/// 结构状态按台账算：锁没过期为升级中，记着失败原因为失败，结构版本与应用带的不同为待升级，否则为最新；读不到为不可达
/// </remarks>
/// <param name="unitOfWork">工作单元</param>
/// <param name="tenantSchemas">租户独立库的结构</param>
/// <param name="options">结构升级配置</param>
/// <param name="clock">时间源</param>
public sealed class SchemaQueries(SqlSugarUnitOfWork unitOfWork, TenantSchemas tenantSchemas, SchemaOptions options, TimeProvider clock)
{
    /// <summary>
    /// 分页列出版本台账
    /// </summary>
    /// <remarks>
    /// 每个库一行：平台库与已就绪的租户独立库；关键字匹配库名、应用版本、结构版本与升级节点
    /// </remarks>
    public async Task<Result<PageResult<SchemaVersionResponse>>> ListVersionsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;
        var rows = new List<SchemaVersionResponse>();
        if (await PlatformVersionAsync(ct).ConfigureAwait(false) is { } platform)
        {
            rows.Add(platform);
        }

        var ready = await tenantSchemas.ReadyTenantsAsync(ct).ConfigureAwait(false);
        if (!ready.TryGetValue(out var tenantIds))
        {
            return ready.Error!;
        }

        foreach (var tenantId in tenantIds)
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
                ("provider", "eq") => query.Where(v => v.Provider == value),
                ("status", "eq") when Enum.TryParse<SchemaVersionStatus>(value, ignoreCase: false, out var status) && Enum.IsDefined(status) => query.Where(v => v.Status == status),
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
                    "status" => static v => v.Status,
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
    /// 取一个库的版本台账
    /// </summary>
    public async Task<Result<SchemaVersionResponse>> GetVersionAsync(string database, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;
        if (string.Equals(database, SchemaVersionResponse.Platform, StringComparison.Ordinal))
        {
            return await PlatformVersionAsync(ct).ConfigureAwait(false) is { } platform ? platform : NotFound(database);
        }

        var known = await IsKnownAsync(database, ct).ConfigureAwait(false);
        if (!known.TryGetValue(out var isKnown))
        {
            return known.Error!;
        }

        return isKnown ? await TenantVersionAsync(database, ct).ConfigureAwait(false) : NotFound(database);
    }

    /// <summary>
    /// 库名是平台库或已就绪的租户独立库
    /// </summary>
    public async Task<Result<bool>> IsKnownAsync(string database, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);
        if (string.Equals(database, SchemaVersionResponse.Platform, StringComparison.Ordinal))
        {
            return true;
        }

        var ready = await tenantSchemas.ReadyTenantsAsync(cancellationToken).ConfigureAwait(false);
        return ready.TryGetValue(out var tenantIds) ? tenantIds.Contains(database, StringComparer.Ordinal) : ready.Error!;
    }

    /// <summary>
    /// 分页列出一个库的升级执行记录
    /// </summary>
    /// <remarks>
    /// 筛选 <c>database</c>（<c>eq</c>，不给为平台库）选库；其余筛选与排序在这个库上做
    /// </remarks>
    public async Task<Result<PageResult<MigrationResponse>>> ListMigrationsAsync(PageRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var database = request.Filters.FirstOrDefault(static f => f is { Field: "database", Operator: "eq" })?.Value ?? SchemaVersionResponse.Platform;
        var known = await IsKnownAsync(database, context.CancellationToken).ConfigureAwait(false);
        if (!known.TryGetValue(out var isKnown))
        {
            return known.Error!;
        }

        if (!isKnown)
        {
            return NotFound(database);
        }

        if (string.Equals(database, SchemaVersionResponse.Platform, StringComparison.Ordinal))
        {
            return await ListMigrationsOnAsync(unitOfWork.ConnectionFor(typeof(SysMigrationHistory)), database, request, context.CancellationToken).ConfigureAwait(false);
        }

        var read = await tenantSchemas.ReadAsync(database, (shard, ct) => ListMigrationsOnAsync(shard, database, request, ct), context.CancellationToken).ConfigureAwait(false);
        return read.TryGetValue(out var listed) ? listed : read.Error!;
    }

    /// <summary>
    /// 取一条执行记录（带执行的 SQL）
    /// </summary>
    public async Task<Result<MigrationDetailResponse>> GetMigrationAsync(Guid id, string? database, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var name = database ?? SchemaVersionResponse.Platform;
        var known = await IsKnownAsync(name, context.CancellationToken).ConfigureAwait(false);
        if (!known.TryGetValue(out var isKnown))
        {
            return known.Error!;
        }

        if (!isKnown)
        {
            return NotFound(name);
        }

        SysMigrationHistory? row;
        if (string.Equals(name, SchemaVersionResponse.Platform, StringComparison.Ordinal))
        {
            row = await unitOfWork.ConnectionFor(typeof(SysMigrationHistory)).Queryable<SysMigrationHistory>().FirstAsync(m => m.Id == id, context.CancellationToken).ConfigureAwait(false);
        }
        else
        {
            var read = await tenantSchemas.ReadAsync(name, async (shard, ct) =>
            {
                var found = await shard.Queryable<SysMigrationHistory>().FirstAsync(m => m.Id == id, ct).ConfigureAwait(false);
                return found is null ? Maybe.None<SysMigrationHistory>() : Maybe.Some(found);
            }, context.CancellationToken).ConfigureAwait(false);
            if (!read.TryGetValue(out var found))
            {
                return read.Error!;
            }

            row = found.TryGetValue(out var value) ? value : null;
        }

        return row is null ? NotFound(id) : MigrationDetailResponse.From(row, name);
    }

    private static async Task<Result<PageResult<MigrationResponse>>> ListMigrationsOnAsync(ISqlSugarClient client, string database, PageRequest request, CancellationToken cancellationToken)
    {
        var query = client.Queryable<SysMigrationHistory>();
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
                ("database", "eq") => query,
                ("version", "eq") => query.Where(m => m.Version == value),
                ("version", "contains") => query.Where(m => m.Version.Contains(value)),
                ("scriptName", "contains") => query.Where(m => m.ScriptName.Contains(value)),
                ("nodeName", "contains") => query.Where(m => m.NodeName != null && m.NodeName.Contains(value)),
                ("success", "eq") when bool.TryParse(value, out var success) => query.Where(m => m.Success == success),
                ("runId", "eq") when Guid.TryParse(value, out var runId) => query.Where(m => m.RunId == runId),
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
        var rows = await query.ToPageListAsync(request.Page.Index, request.Page.Size, total, cancellationToken).ConfigureAwait(false);
        return new PageResult<MigrationResponse>([.. rows.Select(r => MigrationResponse.From(r, database))], total.Value, request.Page);
    }

    /// <summary>
    /// 平台库的台账行
    /// </summary>
    /// <remarks>
    /// 还没跑过升级执行器（只开 InitTables 的开发与测试库）的库没有台账行，不列
    /// </remarks>
    private async Task<SchemaVersionResponse?> PlatformVersionAsync(CancellationToken cancellationToken)
    {
        var client = unitOfWork.ConnectionFor(typeof(SysVersion));
        var row = await client.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, cancellationToken).ConfigureAwait(false);
        return row is null ? null : Project(row, SchemaVersionResponse.Platform, client);
    }

    private async Task<SchemaVersionResponse> TenantVersionAsync(string tenantId, CancellationToken cancellationToken)
    {
        var read = await tenantSchemas.ReadAsync(tenantId, async (shard, ct) =>
        {
            var row = await shard.Queryable<SysVersion>().FirstAsync(v => v.Id == SysVersion.RowId, ct).ConfigureAwait(false);
            return row is null
                ? SchemaVersionResponse.Unreadable(tenantId, $"The database of tenant '{tenantId}' has no schema version ledger.")
                : Project(row, tenantId, shard);
        }, cancellationToken).ConfigureAwait(false);
        return read.TryGetValue(out var version) ? version : SchemaVersionResponse.Unreadable(tenantId, read.Error!.Message);
    }

    private SchemaVersionResponse Project(SysVersion row, string database, ISqlSugarClient client)
    {
        var provider = DatabaseProviders.NameOf(client.CurrentConnectionConfig.DbType);
        var discovered = SchemaScripts.Discover(options.Directory, provider);
        var target = discovered.TryGetValue(out var versions) ? SchemaScripts.NameOf(SchemaScripts.Current(versions)) : null;
        return SchemaVersionResponse.From(row, database, provider, target ?? string.Empty, clock.GetUtcNow() - options.LockExpiry);
    }

    private static bool Has(string? value, string keyword) => value is not null && value.Contains(keyword, StringComparison.OrdinalIgnoreCase);

    private static Error NotFound(string database)
        => Error.NotFound(MaintenanceErrors.SchemaRecordNotFound, MaintenanceMessages.SchemaRecordNotFound, MessageArguments.Of(("id", database)));

    private static Error NotFound(Guid id)
        => Error.NotFound(MaintenanceErrors.SchemaRecordNotFound, MaintenanceMessages.SchemaRecordNotFound, MessageArguments.Of(("id", id)));

    private static Error FilterUnsupported(PageFilter filter)
        => Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.FilterUnsupported, MessageArguments.Of(("field", filter.Field), ("operator", filter.Operator)));

    private static Error SortUnsupported(PageSort sort)
        => Error.Validation(MaintenanceErrors.QueryUnsupported, MaintenanceMessages.SortUnsupported, MessageArguments.Of(("field", sort.Field)));

    private static DateTimeOffset? TimeOf(string value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var time) ? time : null;
}
