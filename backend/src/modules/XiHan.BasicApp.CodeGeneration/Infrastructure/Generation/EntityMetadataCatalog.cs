#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EntityMetadataCatalog
// Guid:c0de9e00-0312-4a00-9000-000000000312
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/21 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 已注册实体元数据目录的实现（反射一次、进程内缓存）
/// </summary>
/// <remarks>
/// 注册为单例：反射结果在进程生命周期内不变。目录同时保留每个表对应的实体 <see cref="Type"/>，
/// 供推断引擎读取类名/命名空间/基类/枚举属性等——这些信息 DB 元数据里没有。
/// </remarks>
public sealed class EntityMetadataCatalog : IEntityMetadataCatalog
{
    private readonly IReadOnlyDictionary<string, string> _tables;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _columns;
    private readonly IReadOnlyDictionary<string, Type> _types;
    private readonly IReadOnlySet<string> _splitBases;

    /// <summary>
    /// 构造函数（反射构建目录）
    /// </summary>
    public EntityMetadataCatalog()
    {
        var tables = new Dictionary<string, string>(StringComparer.Ordinal);
        var columns = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        var types = new Dictionary<string, Type>(StringComparer.Ordinal);
        var splitBases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            var assemblyName = assembly.GetName().Name;
            if (assemblyName is null || !assemblyName.StartsWith("XiHan", StringComparison.Ordinal))
            {
                continue;
            }

            Type?[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                assemblyTypes = ex.Types;
            }
            catch
            {
                continue;
            }

            foreach (var type in assemblyTypes)
            {
                if (type is null || !type.IsClass)
                {
                    continue;
                }

                var tableAttribute = type.GetCustomAttribute<SugarTable>();
                if (tableAttribute is null)
                {
                    continue;
                }

                // 分表实体的 SugarTable 名是模板（如 SysDiffLog_{year}{month}{day}），取占位符 { 之前的基础名
                var rawTableName = string.IsNullOrWhiteSpace(tableAttribute.TableName) ? type.Name : tableAttribute.TableName;
                var realTable = ExtractBaseName(rawTableName);
                if (string.IsNullOrWhiteSpace(realTable))
                {
                    realTable = type.Name;
                }

                var lowerTable = realTable.ToLowerInvariant();
                tables[lowerTable] = realTable;
                types[lowerTable] = type;

                // 分表标记：ISplitTableEntity 接口 或 [SplitTable] 特性（与框架自身判定一致）
                var isSplitTable = typeof(ISplitTableEntity).IsAssignableFrom(type)
                    || type.GetCustomAttribute<SplitTableAttribute>() is not null;
                if (isSplitTable)
                {
                    splitBases.Add(lowerTable);
                }

                var columnMap = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var columnAttribute = property.GetCustomAttribute<SugarColumn>();
                    if (columnAttribute is not null && columnAttribute.IsIgnore)
                    {
                        continue;
                    }

                    var realColumn = columnAttribute is not null && !string.IsNullOrWhiteSpace(columnAttribute.ColumnName)
                        ? columnAttribute.ColumnName
                        : property.Name;
                    columnMap[realColumn.ToLowerInvariant()] = realColumn;
                }

                columns[lowerTable] = columnMap;
            }
        }

        _tables = tables;
        _columns = columns;
        _types = types;
        _splitBases = splitBases;
    }

    /// <inheritdoc />
    public string ResolveTable(string dbTableName)
    {
        if (string.IsNullOrWhiteSpace(dbTableName))
        {
            return dbTableName;
        }

        return _tables.TryGetValue(dbTableName.ToLowerInvariant(), out var real) ? real : dbTableName;
    }

    /// <inheritdoc />
    public string ResolveLogical(string dbTableName)
    {
        if (string.IsNullOrWhiteSpace(dbTableName))
        {
            return dbTableName;
        }

        if (_tables.TryGetValue(dbTableName.ToLowerInvariant(), out var exact))
        {
            return exact;
        }

        return TryResolveSplitShard(dbTableName, out var baseName) ? baseName : dbTableName;
    }

    /// <inheritdoc />
    public string ResolveColumn(string realTableName, string dbColumnName)
    {
        if (string.IsNullOrWhiteSpace(realTableName) || string.IsNullOrWhiteSpace(dbColumnName))
        {
            return dbColumnName;
        }

        return _columns.TryGetValue(realTableName.ToLowerInvariant(), out var map)
            && map.TryGetValue(dbColumnName.ToLowerInvariant(), out var real)
            ? real
            : dbColumnName;
    }

    /// <inheritdoc />
    public bool IsSplitBase(string tableName)
    {
        return !string.IsNullOrWhiteSpace(tableName) && _splitBases.Contains(tableName.ToLowerInvariant());
    }

    /// <inheritdoc />
    public bool TryResolveSplitShard(string dbTableName, out string baseRealName)
    {
        baseRealName = string.Empty;
        if (string.IsNullOrWhiteSpace(dbTableName))
        {
            return false;
        }

        var lower = dbTableName.ToLowerInvariant();
        foreach (var splitBase in _splitBases)
        {
            // 分片命名约定：基础名_后缀（SqlSugar 默认 {table}_{yyyyMMdd}）
            if (lower.Length > splitBase.Length + 1
                && lower.StartsWith(splitBase, StringComparison.Ordinal)
                && lower[splitBase.Length] == '_'
                && _tables.TryGetValue(splitBase, out var real))
            {
                baseRealName = real;
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryGetEntityType(string tableName, out Type entityType)
    {
        entityType = typeof(object);
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return false;
        }

        var lower = tableName.ToLowerInvariant();
        if (_types.TryGetValue(lower, out var type))
        {
            entityType = type;
            return true;
        }

        // 传入的是物理分片名：折叠为基础名再取类型
        if (TryResolveSplitShard(tableName, out var baseName)
            && _types.TryGetValue(baseName.ToLowerInvariant(), out var baseType))
        {
            entityType = baseType;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 取表名模板占位符 { 之前的基础名（如 SysDiffLog_{year}{month}{day} → SysDiffLog）；无占位符则原样
    /// </summary>
    private static string ExtractBaseName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return tableName;
        }

        var placeholderIndex = tableName.IndexOf('{');
        return placeholderIndex < 0
            ? tableName
            : tableName[..placeholderIndex].TrimEnd('_', '-', ' ');
    }
}
