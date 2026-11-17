// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.BasicApp.Runtime.Persistence;

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 算出一个库的结构升级预览
/// </summary>
/// <remarks>
/// 只读：读台账、读库里的表、列与索引，与实体比对。与升级的口径一致：脚本取比台账新的本库类型目录，ORM 只建缺的表、加缺的列与索引
/// 分表实体不列：它的表随写入按周期建出
/// </remarks>
internal static class SchemaPlanning
{
    /// <summary>
    /// 算出预览
    /// </summary>
    /// <param name="database">库名</param>
    /// <param name="ledger">台账与脚本所在的连接</param>
    /// <param name="groups">实体按落点分到各自的连接上</param>
    /// <param name="versions">脚本目录里的版本（已按这个库的类型取了脚本）</param>
    /// <param name="tenant">是否租户独立库</param>
    public static SchemaPlan Plan(string database, ISqlSugarClient ledger, IEnumerable<IGrouping<ISqlSugarClient, Type>> groups, IReadOnlyList<SchemaVersionScripts> versions, bool tenant)
    {
        var provider = DatabaseProviders.NameOf(ledger.CurrentConnectionConfig.DbType);
        var target = SchemaScripts.Current(versions);
        var versionTable = ledger.EntityMaintenance.GetEntityInfo<SysVersion>().DbTableName;
        var row = ledger.DbMaintenance.IsAnyTable(versionTable, false) ? ledger.Queryable<SysVersion>().First(v => v.Id == SysVersion.RowId) : null;
        var from = row?.DbVersion;

        var scripts = new List<SchemaPlanScript>();
        if (from is not null && Version.TryParse(from, out var current))
        {
            foreach (var version in versions.Where(v => v.Version > current))
            {
                foreach (var file in tenant ? version.TenantFiles : version.Files)
                {
                    var script = provider + "/" + (tenant ? SchemaScripts.TenantDirectory + "/" : string.Empty) + Path.GetFileName(file);
                    scripts.Add(new SchemaPlanScript(version.Name, script, File.ReadAllText(file)));
                }
            }
        }

        var tables = new List<SchemaPlanTable>();
        var indexes = new List<SchemaPlanIndex>();
        foreach (var group in groups)
        {
            Compare(group.Key, group, tables, indexes);
        }

        var syncDue = row is null || from is null || scripts.Count > 0 || !string.Equals(row.AppVersion, AppInfo.Version, StringComparison.Ordinal);
        return new SchemaPlan(database, provider, from, SchemaScripts.NameOf(target), syncDue, scripts, tables, indexes);
    }

    private static void Compare(ISqlSugarClient client, IEnumerable<Type> types, List<SchemaPlanTable> tables, List<SchemaPlanIndex> indexes)
    {
        var existing = client.DbMaintenance.GetTableInfoList(false).Select(static t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var type in types.Where(static t => !t.IsDefined(typeof(SplitTableAttribute), inherit: false)))
        {
            var entity = client.EntityMaintenance.GetEntityInfo(type);
            var table = entity.DbTableName;
            var columns = entity.Columns.Where(static c => !c.IsIgnore).ToList();
            var isNew = !existing.Contains(table);
            if (isNew)
            {
                tables.Add(new SchemaPlanTable(table, true, [.. columns.Select(c => Describe(client, c))]));
            }
            else
            {
                var present = client.DbMaintenance.GetColumnInfosByTableName(table, false).Select(static c => c.DbColumnName).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var missing = columns.Where(c => !present.Contains(c.DbColumnName)).Select(c => Describe(client, c)).ToList();
                if (missing.Count > 0)
                {
                    tables.Add(new SchemaPlanTable(table, false, missing));
                }
            }

            var names = (entity.Indexs ?? []).Select(static i => i.IndexName)
                .Concat(type.GetCustomAttributes<ActiveUniqueIndexAttribute>(inherit: false).Select(static a => a.Name).OfType<string>());
            foreach (var name in names.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (isNew || !client.DbMaintenance.IsAnyIndex(name))
                {
                    indexes.Add(new SchemaPlanIndex(table, name));
                }
            }
        }
    }

    /// <summary>
    /// 列在这个库上的类型
    /// </summary>
    /// <remarks>
    /// 实体或约定给了类型即用它（<c>[LongText]</c> 的候选取这个库认的那个），没给即按属性类型取这个库的缺省类型名
    /// </remarks>
    private static SchemaPlanColumn Describe(ISqlSugarClient client, EntityColumnInfo column)
    {
        var bind = client.Ado.DbBind;
        var dataType = column.DataType;
        if (string.IsNullOrEmpty(dataType))
        {
            var type = Nullable.GetUnderlyingType(column.PropertyInfo.PropertyType) ?? column.PropertyInfo.PropertyType;
            dataType = bind.GetDbTypeName((type.IsEnum ? Enum.GetUnderlyingType(type) : type).Name);
        }
        else if (dataType.Contains(',', StringComparison.Ordinal))
        {
            dataType = dataType.Split(',').FirstOrDefault(candidate => bind.MappingTypes.Any(m => m.Key.Equals(candidate, StringComparison.OrdinalIgnoreCase))) ?? dataType;
        }

        var size = column.Length > 0 ? column.DecimalDigits > 0 ? $"({column.Length},{column.DecimalDigits})" : $"({column.Length})" : string.Empty;
        return new SchemaPlanColumn(column.DbColumnName, dataType + size, column.IsNullable);
    }
}
