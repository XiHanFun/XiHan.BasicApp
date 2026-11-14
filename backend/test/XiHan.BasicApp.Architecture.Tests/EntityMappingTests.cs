// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using SqlSugar;
using XiHan.BasicApp.Architecture.Tests.Model;
using XiHan.BasicApp.Contracts.Entities;
using XiHan.Framework.Domain.Entities;
using XiHan.Framework.Persistence.Abstractions;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查实体的显式映射
/// </summary>
/// <remarks>
/// 表名与列名不靠命名约定从类名、属性名推出（缩写会被拆错：<c>SysOAuthApp</c> 推成 <c>sys_o_auth_app</c>）
/// 实体写 <c>[SugarTable]</c>（表名、表说明）；映射到列的属性写 <c>[SugarColumn]</c>（列名、可空、说明），可空与属性类型一致，字符串列写长度或标 <c>[LongText]</c>，长度只写在 <c>[SugarColumn]</c> 里
/// 名字都是小写下划线；索引名明确写出：<c>[SugarIndex]</c> 是 <c>ix_</c> / <c>ux_</c> 加表名（按月分表用 <c>{split_table}</c>）再加列，<c>[ActiveUniqueIndex]</c> 写 <c>Name</c>，是 <c>ux_</c> 加表名再加列
/// </remarks>
public sealed partial class EntityMappingTests
{
    private const string SplitSuffix = "_{year}{month}{day}";

    private static readonly NullabilityInfoContext _nullability = new();

    [Fact]
    public void Entities_DeclareTheirTable_Columns_AndIndexNames()
    {
        var entities = Packages.Assemblies.SelectMany(static a => a.GetTypes())
            .Where(static t => t is { IsClass: true, IsAbstract: false } && typeof(IEntity<Guid>).IsAssignableFrom(t))
            .ToList();
        Assert.True(entities.Count >= 80, $"Expected the BasicApp entities, found {entities.Count}.");
        var problems = new List<string>();
        var tables = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var type in entities)
        {
            var table = type.GetCustomAttribute<SugarTable>(inherit: false);
            if (table is null || string.IsNullOrWhiteSpace(table.TableName))
            {
                problems.Add($"{type.FullName}: no [SugarTable] with a table name.");
                continue;
            }

            var split = type.IsDefined(typeof(SplitTableAttribute), inherit: false);
            if (split && !table.TableName.EndsWith(SplitSuffix, StringComparison.Ordinal))
            {
                problems.Add($"{type.Name}: split table '{table.TableName}' does not end with '{SplitSuffix}'.");
            }

            var tableName = split ? table.TableName[..^SplitSuffix.Length] : table.TableName;
            if (!SnakeCase().IsMatch(tableName))
            {
                problems.Add($"{type.Name}: table name '{table.TableName}' is not lower snake_case.");
            }

            if (!tables.TryAdd(tableName, type.Name))
            {
                problems.Add($"{type.Name}: table name '{tableName}' is also used by {tables[tableName]}.");
            }

            if (string.IsNullOrWhiteSpace(table.TableDescription))
            {
                problems.Add($"{type.Name}: [SugarTable] has no TableDescription.");
            }

            problems.AddRange(ColumnProblems(type));
            problems.AddRange(IndexProblems(type, split ? "{split_table}" : table.TableName, table.TableName));
        }

        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    private static IEnumerable<string> ColumnProblems(Type type)
    {
        var columns = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in MappedProperties(type))
        {
            var label = $"{type.Name}.{property.Name}";
            if (property.IsDefined(typeof(StringLengthAttribute), inherit: true) || property.IsDefined(typeof(MaxLengthAttribute), inherit: true))
            {
                yield return $"{label}: length goes into [SugarColumn(Length = …)], not [StringLength] / [MaxLength].";
            }

            var column = property.GetCustomAttribute<SugarColumn>(inherit: true);
            if (column is null || string.IsNullOrWhiteSpace(column.ColumnName))
            {
                yield return $"{label}: no [SugarColumn] with a column name.";
                continue;
            }

            if (!SnakeCase().IsMatch(column.ColumnName))
            {
                yield return $"{label}: column name '{column.ColumnName}' is not lower snake_case.";
            }

            if (!columns.TryAdd(column.ColumnName, property.Name))
            {
                yield return $"{label}: column name '{column.ColumnName}' is also used by {columns[column.ColumnName]}.";
            }

            if (string.IsNullOrWhiteSpace(column.ColumnDescription))
            {
                yield return $"{label}: [SugarColumn] has no ColumnDescription.";
            }

            if (column.IsNullable != IsNullable(property))
            {
                yield return $"{label}: IsNullable = {column.IsNullable} does not match the property type.";
            }

            if (string.Equals(property.Name, nameof(IEntity<>.Id), StringComparison.Ordinal) && !column.IsPrimaryKey)
            {
                yield return $"{label}: the key column is not marked IsPrimaryKey.";
            }

            var longText = property.IsDefined(typeof(LongTextAttribute), inherit: true);
            var hasLength = column.Length > 0;
            if (property.PropertyType == typeof(string))
            {
                if (hasLength == longText)
                {
                    yield return $"{label}: a string column declares either Length or [LongText], not both or neither.";
                }
            }
            else if (hasLength || longText)
            {
                yield return $"{label}: Length / [LongText] on a non-string column.";
            }
        }
    }

    private static IEnumerable<string> IndexProblems(Type type, string tableToken, string tableName)
    {
        foreach (var index in type.GetCustomAttributes<SugarIndexAttribute>(inherit: false))
        {
            var prefix = (index.IsUnique ? "ux_" : "ix_") + tableToken + "_";
            if (!index.IndexName.StartsWith(prefix, StringComparison.Ordinal) || !SnakeCase().IsMatch(index.IndexName[prefix.Length..]))
            {
                yield return $"{type.Name}: [SugarIndex] '{index.IndexName}' is not named '{prefix}<columns>'.";
            }
        }

        foreach (var index in type.GetCustomAttributes<ActiveUniqueIndexAttribute>(inherit: false))
        {
            var prefix = "ux_" + tableName + "_";
            if (index.Name is null || !index.Name.StartsWith(prefix, StringComparison.Ordinal) || !SnakeCase().IsMatch(index.Name[prefix.Length..]))
            {
                yield return $"{type.Name}: [ActiveUniqueIndex] on ({string.Join(", ", index.Columns)}) has no Name '{prefix}<columns>'.";
            }
        }
    }

    // 与框架的列约定同一口径：有公开 setter、不是集合（字符串与字节数组除外）、没标忽略
    private static IEnumerable<PropertyInfo> MappedProperties(Type type)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(static p => p.SetMethod is { IsPublic: true }
                && (p.PropertyType == typeof(string) || p.PropertyType == typeof(byte[]) || !typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                && p.GetCustomAttribute<SugarColumn>(inherit: true) is not { IsIgnore: true });

    private static bool IsNullable(PropertyInfo property)
    {
        if (property.PropertyType.IsValueType)
        {
            return Nullable.GetUnderlyingType(property.PropertyType) is not null;
        }

        lock (_nullability)
        {
            return _nullability.Create(property).WriteState == NullabilityState.Nullable;
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(_[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SnakeCase();
}
