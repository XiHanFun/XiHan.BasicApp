// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.DataSources;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.Schema;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 一张表的读库来源。
/// </summary>
/// <param name="DataSourceId">数据源 ID，为空即主库。</param>
/// <param name="Definition">外部库的连接定义，主库为空。</param>
/// <param name="DatabaseType">数据库类型。</param>
public sealed record CodeGenTableSource(Guid? DataSourceId, DataSourceDefinition? Definition, CodeGenDatabaseType DatabaseType);

/// <summary>
/// 同步结构的结果。
/// </summary>
/// <param name="Added">新增的列。</param>
/// <param name="Updated">更新的列数。</param>
/// <param name="Removed">移除的列。</param>
public sealed record CodeGenSyncResult(IReadOnlyList<string> Added, int Updated, IReadOnlyList<string> Removed);

/// <summary>
/// 表配置的导入、同步结构与规则。
/// </summary>
/// <remarks>
/// 读库结构经框架的 <see cref="ISchemaReader"/>（主库传空，外部库传数据源的连接定义），表名照返回的原名、不自己转大小写（修订（八十三））。
/// 导入照 4.x 按表隔离：一张表读不到、已导入或没有列就记一条失败，其余照常导入；同步结构照 4.x 三路合并：结构字段以库为准，
/// 推断字段只覆盖没被人工改过的，库里已删的列删掉列配置（6.0 软删除）。
/// </remarks>
/// <param name="tables">表配置仓储。</param>
/// <param name="columns">列配置仓储。</param>
/// <param name="dataSources">数据源规则。</param>
/// <param name="reader">框架的读库结构。</param>
/// <param name="inference">推断。</param>
/// <param name="unitOfWork">工作单元。</param>
/// <param name="ids">标识生成器。</param>
public sealed class CodeGenTableService(
    IRepository<SysCodeGenTable, Guid> tables,
    IRepository<SysCodeGenTableColumn, Guid> columns,
    CodeGenDataSourceService dataSources,
    ISchemaReader reader,
    CodeGenInference inference,
    SqlSugarUnitOfWork unitOfWork,
    IIdGenerator<Guid> ids)
{
    /// <summary>
    /// 被跟踪的表字段。
    /// </summary>
    /// <remarks>
    /// 照 4.x：保存时值变了即记为人工改过，同步结构时不覆盖。
    /// </remarks>
    public static IReadOnlyList<string> TrackedTableFields { get; } =
    [
        nameof(SysCodeGenTable.ClassName),
        nameof(SysCodeGenTable.Namespace),
        nameof(SysCodeGenTable.ModuleName),
        nameof(SysCodeGenTable.BusinessName),
        nameof(SysCodeGenTable.FunctionName),
        nameof(SysCodeGenTable.Author),
        nameof(SysCodeGenTable.TemplateType),
        nameof(SysCodeGenTable.TreeParentColumn),
        nameof(SysCodeGenTable.TreeNameColumn),
    ];

    /// <summary>
    /// 被跟踪的列字段。
    /// </summary>
    public static IReadOnlyList<string> TrackedColumnFields { get; } =
    [
        nameof(SysCodeGenTableColumn.CSharpType),
        nameof(SysCodeGenTableColumn.CSharpProperty),
        nameof(SysCodeGenTableColumn.TsType),
        nameof(SysCodeGenTableColumn.IsRequired),
        nameof(SysCodeGenTableColumn.IsUnique),
        nameof(SysCodeGenTableColumn.IsList),
        nameof(SysCodeGenTableColumn.IsInsert),
        nameof(SysCodeGenTableColumn.IsEdit),
        nameof(SysCodeGenTableColumn.IsQuery),
        nameof(SysCodeGenTableColumn.QueryType),
        nameof(SysCodeGenTableColumn.HtmlType),
        nameof(SysCodeGenTableColumn.DictSelectorType),
        nameof(SysCodeGenTableColumn.DictCode),
        nameof(SysCodeGenTableColumn.EnumTypeName),
        nameof(SysCodeGenTableColumn.ConstValues),
        nameof(SysCodeGenTableColumn.RelationTableId),
        nameof(SysCodeGenTableColumn.RelationLabelColumn),
        nameof(SysCodeGenTableColumn.Order),
    ];

    /// <summary>
    /// 不存在。
    /// </summary>
    /// <param name="id">表配置 ID。</param>
    public static Error NotFound(Guid id)
        => Error.NotFound(CodeGenerationErrors.TableNotFound, CodeGenerationMessages.TableNotFound, MessageArguments.Of(("id", id)));

    /// <summary>
    /// 按 ID 取表配置。
    /// </summary>
    /// <param name="id">表配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result<SysCodeGenTable>> FindAsync(Guid id, CancellationToken cancellationToken)
        => (await tables.FindAsync(id, cancellationToken).ConfigureAwait(false)).TryGetValue(out var table) ? table : NotFound(id);

    /// <summary>
    /// 一张表的全部列配置，按排序号。
    /// </summary>
    /// <param name="tableId">表配置 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<IReadOnlyList<SysCodeGenTableColumn>> ColumnsOfAsync(Guid tableId, CancellationToken cancellationToken)
    {
        var rows = await columns.ListAsync(EntityQuery.All<SysCodeGenTableColumn>().Where(new Specification<SysCodeGenTableColumn>(c => c.TableId == tableId)), cancellationToken).ConfigureAwait(false);
        return [.. rows.OrderBy(static c => c.Order).ThenBy(static c => c.ColumnName, StringComparer.Ordinal)];
    }

    /// <summary>
    /// 表名是否已被别的表配置占用。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <param name="except">排除的表配置（修改时是它自己）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<bool> TableNameTakenAsync(string tableName, Guid? except, CancellationToken cancellationToken)
    {
        var excluded = except ?? Guid.Empty;
        return await tables.CountAsync(EntityQuery.All<SysCodeGenTable>().Where(new Specification<SysCodeGenTable>(t => t.TableName == tableName && t.Id != excluded)), cancellationToken).ConfigureAwait(false) > 0;
    }

    /// <summary>
    /// 解析读库来源。
    /// </summary>
    /// <remarks>
    /// 数据源为空即主库（数据库类型取主库的连接）；外部库要存在且启用，口令解不开即失败。
    /// </remarks>
    /// <param name="dataSourceId">数据源 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result<CodeGenTableSource>> SourceOfAsync(Guid? dataSourceId, CancellationToken cancellationToken)
    {
        if (!dataSourceId.HasValue)
        {
            var primary = PrimaryDatabaseType();
            return primary.TryGetValue(out var type) ? new CodeGenTableSource(null, null, type) : primary.Error!;
        }

        var found = await dataSources.FindAsync(dataSourceId.Value, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var source))
        {
            return found.Error!;
        }

        if (source.Status != CodeGenStatus.Active)
        {
            return Error.Conflict(CodeGenerationErrors.DataSourceDisabled, CodeGenerationMessages.DataSourceDisabled, MessageArguments.Of(("name", source.SourceName)));
        }

        var definition = dataSources.DefinitionOf(source);
        return definition.TryGetValue(out var defined) ? new CodeGenTableSource(source.Id, defined, source.DatabaseType) : definition.Error!;
    }

    /// <summary>
    /// 分页列出库里的表。
    /// </summary>
    /// <param name="source">读库来源。</param>
    /// <param name="keyword">关键字（匹配表名与注释）。</param>
    /// <param name="skip">跳过的条数。</param>
    /// <param name="take">取的条数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public Task<Result<TablePage>> ListDatabaseTablesAsync(CodeGenTableSource source, string? keyword, int skip, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        return reader.ListTablesAsync(source.Definition, new TableListQuery { Keyword = keyword, Skip = skip, Take = take }, cancellationToken);
    }

    /// <summary>
    /// 导入一批表。
    /// </summary>
    /// <param name="source">读库来源。</param>
    /// <param name="tableNames">表名（照列表返回的原名）。</param>
    /// <param name="author">作者。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>每张表的结果，与入参顺序一致；整批读不了为失败。</returns>
    public async Task<Result<IReadOnlyList<(string TableName, Result Outcome)>>> ImportAsync(CodeGenTableSource source, IReadOnlyList<string> tableNames, string? author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(tableNames);
        var read = await reader.GetTablesAsync(source.Definition, tableNames, cancellationToken).ConfigureAwait(false);
        if (!read.TryGetValue(out var schemas))
        {
            return read.Error!;
        }

        var outcomes = new List<(string, Result)>(tableNames.Count);
        for (var i = 0; i < tableNames.Count; i++)
        {
            outcomes.Add((tableNames[i], await ImportOneAsync(source, tableNames[i], schemas[i], author, cancellationToken).ConfigureAwait(false)));
        }

        return outcomes;
    }

    /// <summary>
    /// 按最新库结构同步一张表的配置。
    /// </summary>
    /// <param name="table">表配置。</param>
    /// <param name="author">作者。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result<CodeGenSyncResult>> SyncAsync(SysCodeGenTable table, string? author, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(table);
        var resolved = await SourceOfAsync(table.DataSourceId, cancellationToken).ConfigureAwait(false);
        if (!resolved.TryGetValue(out var source))
        {
            return resolved.Error!;
        }

        var read = await reader.GetTableAsync(source.Definition, table.TableName, cancellationToken).ConfigureAwait(false);
        if (!read.TryGetValue(out var schema))
        {
            return read.Error!;
        }

        var suggestion = await inference.InferAsync(schema, table.DatabaseType, source.DataSourceId is null, author, cancellationToken).ConfigureAwait(false);

        // 表级：表注释与主键以库为准，被跟踪的字段没人工改过才跟随推断
        table.TableComment = schema.Table.Comment;
        table.PrimaryKeyColumn = suggestion.PrimaryKeyColumn;
        SyncField(table, nameof(SysCodeGenTable.ClassName), () => table.ClassName = suggestion.ClassName);
        SyncField(table, nameof(SysCodeGenTable.Namespace), () => table.Namespace = suggestion.Namespace);
        SyncField(table, nameof(SysCodeGenTable.ModuleName), () => table.ModuleName = suggestion.ModuleName);
        SyncField(table, nameof(SysCodeGenTable.BusinessName), () => table.BusinessName = suggestion.BusinessName);
        SyncField(table, nameof(SysCodeGenTable.FunctionName), () => table.FunctionName = suggestion.FunctionName);
        var updatedTable = await tables.UpdateAsync(table, cancellationToken).ConfigureAwait(false);
        if (updatedTable.IsFailure)
        {
            return updatedTable.Error!;
        }

        // 列级三路合并
        var existing = await ColumnsOfAsync(table.Id, cancellationToken).ConfigureAwait(false);
        var byName = existing.ToDictionary(static c => c.ColumnName, StringComparer.OrdinalIgnoreCase);
        var latest = suggestion.Columns.Select(static c => c.ColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = new List<string>();
        var updated = 0;
        foreach (var inferred in suggestion.Columns)
        {
            if (byName.TryGetValue(inferred.ColumnName, out var current))
            {
                if (!Merge(current, inferred))
                {
                    continue;
                }

                var saved = await columns.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
                if (saved.IsFailure)
                {
                    return saved.Error!;
                }

                updated++;
                continue;
            }

            var addedColumn = await columns.AddAsync(ColumnOf(table.Id, inferred), cancellationToken).ConfigureAwait(false);
            if (addedColumn.IsFailure)
            {
                return addedColumn.Error!;
            }

            added.Add(inferred.ColumnName);
        }

        var removed = new List<string>();
        foreach (var current in existing.Where(c => !latest.Contains(c.ColumnName)))
        {
            var deleted = await columns.RemoveAsync(current, cancellationToken).ConfigureAwait(false);
            if (deleted.IsFailure)
            {
                return deleted.Error!;
            }

            removed.Add(current.ColumnName);
        }

        return new CodeGenSyncResult(added, updated, removed);
    }

    /// <summary>
    /// 删除表配置与它的列配置（软删除）。
    /// </summary>
    /// <param name="table">表配置。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result> RemoveAsync(SysCodeGenTable table, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(table);
        foreach (var column in await ColumnsOfAsync(table.Id, cancellationToken).ConfigureAwait(false))
        {
            var removed = await columns.RemoveAsync(column, cancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed;
            }
        }

        return await tables.RemoveAsync(table, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> ImportOneAsync(CodeGenTableSource source, string tableName, Result<TableSchema> read, string? author, CancellationToken cancellationToken)
    {
        if (!read.TryGetValue(out var schema))
        {
            return read.Error!;
        }

        if (await TableNameTakenAsync(schema.Table.Name, null, cancellationToken).ConfigureAwait(false))
        {
            return Error.Conflict(CodeGenerationErrors.TableAlreadyImported, CodeGenerationMessages.TableAlreadyImported, MessageArguments.Of(("table", schema.Table.Name)));
        }

        // 先判空再落库：没有列的表配置生成不出东西（照 4.x）
        if (schema.Columns.Count == 0)
        {
            return Error.Validation(CodeGenerationErrors.TableHasNoColumn, CodeGenerationMessages.TableHasNoColumn, MessageArguments.Of(("table", tableName)));
        }

        var suggestion = await inference.InferAsync(schema, source.DatabaseType, source.DataSourceId is null, author, cancellationToken).ConfigureAwait(false);
        var table = new SysCodeGenTable
        {
            Id = ids.NewId(),
            TableName = schema.Table.Name,
            TableComment = schema.Table.Comment,
            ClassName = suggestion.ClassName,
            Namespace = suggestion.Namespace,
            ModuleName = suggestion.ModuleName,
            BusinessName = suggestion.BusinessName,
            FunctionName = suggestion.FunctionName,
            Author = suggestion.Author,
            DatabaseType = source.DatabaseType,
            // 记住来源：同步结构、生成与零代码运行时据此回到同一个库
            DataSourceId = source.DataSourceId,
            PrimaryKeyColumn = suggestion.PrimaryKeyColumn,
            TemplateType = suggestion.TemplateType,
            TreeParentColumn = suggestion.TreeParentColumn,
            TreeNameColumn = suggestion.TreeNameColumn,
            // 沿用已有实体时生成到它所在的包（照 4.x），否则缺省新建包
            Target = suggestion.FromRegisteredEntity ? CodeGenTarget.ExistingPackage : CodeGenTarget.NewPackage,
        };
        var added = await tables.AddAsync(table, cancellationToken).ConfigureAwait(false);
        if (added.IsFailure)
        {
            return added;
        }

        var results = await columns.AddRangeAsync([.. suggestion.Columns.Select(c => ColumnOf(table.Id, c))], cancellationToken).ConfigureAwait(false);
        foreach (var result in results)
        {
            if (result.IsFailure)
            {
                // 按表隔离、整批同一事务：列写不进就把这张表的配置删掉，免得留下没有列的表配置（照 4.x）
                _ = await tables.RemoveAsync(table, cancellationToken).ConfigureAwait(false);
                return result;
            }
        }

        return Result.Success();
    }

    private SysCodeGenTableColumn ColumnOf(Guid tableId, CodeGenColumnSuggestion column) => new()
    {
        Id = ids.NewId(),
        TableId = tableId,
        ColumnName = column.ColumnName,
        ColumnComment = column.ColumnComment,
        ColumnType = column.ColumnType,
        CSharpType = column.CSharpType,
        CSharpProperty = column.CSharpProperty,
        TsType = column.TsType,
        HtmlType = column.HtmlType,
        QueryType = column.QueryType,
        DictSelectorType = column.DictSelectorType,
        EnumTypeName = column.EnumTypeName,
        ColumnLength = column.Length,
        DecimalDigits = column.DecimalDigits,
        IsPrimaryKey = column.IsPrimaryKey,
        IsIdentity = column.IsIdentity,
        IsNullable = column.IsNullable,
        IsRequired = column.IsRequired,
        IsList = column.IsList,
        IsInsert = column.IsInsert,
        IsEdit = column.IsEdit,
        IsQuery = column.IsQuery,
        Order = column.Order,
    };

    private Result<CodeGenDatabaseType> PrimaryDatabaseType() => unitOfWork.ConnectionFor(typeof(SysCodeGenTable)).CurrentConnectionConfig.DbType switch
    {
        DbType.PostgreSQL => CodeGenDatabaseType.PostgreSql,
        DbType.MySql => CodeGenDatabaseType.MySql,
        DbType.SqlServer => CodeGenDatabaseType.SqlServer,
        DbType.Oracle => CodeGenDatabaseType.Oracle,
        DbType.Sqlite => CodeGenDatabaseType.Sqlite,
        var other => Error.Unavailable(CodeGenerationErrors.DatabaseUnsupported, CodeGenerationMessages.DatabaseUnsupported, MessageArguments.Of(("type", other.ToString()))),
    };

    private static void SyncField(SysCodeGenTable table, string field, Action apply)
    {
        if (!CodeGenModifiedFields.Contains(table.UserModifiedFields, field))
        {
            apply();
        }
    }

    private static bool Merge(SysCodeGenTableColumn current, CodeGenColumnSuggestion inferred)
    {
        var modified = CodeGenModifiedFields.Parse(current.UserModifiedFields);
        var changed = false;

        // 结构字段以库为准，不受人工修改影响
        changed |= Set(current.ColumnType, inferred.ColumnType, v => current.ColumnType = v);
        changed |= Set(current.ColumnComment, inferred.ColumnComment, v => current.ColumnComment = v);
        changed |= Set(current.IsPrimaryKey, inferred.IsPrimaryKey, v => current.IsPrimaryKey = v);
        changed |= Set(current.IsIdentity, inferred.IsIdentity, v => current.IsIdentity = v);
        changed |= Set(current.IsNullable, inferred.IsNullable, v => current.IsNullable = v);
        changed |= Set(current.ColumnLength, inferred.Length, v => current.ColumnLength = v);
        changed |= Set(current.DecimalDigits, inferred.DecimalDigits, v => current.DecimalDigits = v);

        // 推断字段：没被人工改过才覆盖（照 4.x 的四个）
        changed |= !modified.Contains(nameof(SysCodeGenTableColumn.CSharpType)) && Set(current.CSharpType, inferred.CSharpType, v => current.CSharpType = v);
        changed |= !modified.Contains(nameof(SysCodeGenTableColumn.TsType)) && Set(current.TsType, inferred.TsType, v => current.TsType = v);
        changed |= !modified.Contains(nameof(SysCodeGenTableColumn.HtmlType)) && Set(current.HtmlType, inferred.HtmlType, v => current.HtmlType = v);
        changed |= !modified.Contains(nameof(SysCodeGenTableColumn.QueryType)) && Set(current.QueryType, inferred.QueryType, v => current.QueryType = v);
        return changed;
    }

    private static bool Set<T>(T current, T value, Action<T> apply)
    {
        if (EqualityComparer<T>.Default.Equals(current, value))
        {
            return false;
        }

        apply(value);
        return true;
    }
}
