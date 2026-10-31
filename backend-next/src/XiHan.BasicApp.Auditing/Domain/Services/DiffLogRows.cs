// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Text;
using System.Text.Json;
using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.Framework.Persistence.SqlSugar;

namespace XiHan.BasicApp.Auditing.Domain.Services;

/// <summary>
/// 把 SqlSugar 差异日志转成数据变更日志。
/// </summary>
/// <remarks>
/// 一条事件可带一批行：每行一条，修改按主键配对前后值，软删除列 <c>is_deleted</c> 由真变假的记为恢复（4.x 同）。
/// 列名含 password / hash / secret / token / salt 的值一律打码；修改里只动了时间戳、操作人、并发戳这类随写入就变的列的，不算数据变更、不记。
/// 前后值手写成 JSON 对象（按值的类型分支，不走反射序列化）。
/// </remarks>
public static class DiffLogRows
{
    /// <summary>
    /// 打码后的值。
    /// </summary>
    public const string Masked = "***";

    private static readonly string[] _sensitive = ["password", "hash", "secret", "token", "salt"];

    private const string SoftDeleteColumn = "is_deleted";

    private static readonly HashSet<string> _noise = new(StringComparer.OrdinalIgnoreCase)
    {
        "concurrency_stamp", "modified_at", "modified_by", "last_login_at", "last_seen_at",
    };

    /// <summary>
    /// 转换一条差异事件。
    /// </summary>
    /// <remarks>
    /// 公共列（时间、租户、调用方）由调用方填。
    /// </remarks>
    public static IEnumerable<SysDiffLog> From(DiffLogModel model, string? operationId)
    {
        ArgumentNullException.ThrowIfNull(model);
        var entityType = (model.BusinessData as SqlSugarDiffLogData)?.EntityType.FullName ?? string.Empty;
        var kind = model.DiffType switch
        {
            DiffType.insert => DiffKind.Insert,
            DiffType.delete => DiffKind.Delete,
            _ => DiffKind.Update,
        };
        var before = model.BeforeData ?? [];
        var after = model.AfterData ?? [];
        var beforeByKey = before.GroupBy(KeyOf, StringComparer.Ordinal).ToDictionary(static g => g.Key, static g => g.First(), StringComparer.Ordinal);
        IEnumerable<(DiffLogTableInfo? Before, DiffLogTableInfo? After)> pairs = kind switch
        {
            DiffKind.Insert => after.Select(static a => ((DiffLogTableInfo?)null, (DiffLogTableInfo?)a)),
            DiffKind.Delete => before.Select(static b => ((DiffLogTableInfo?)b, (DiffLogTableInfo?)null)),
            _ => after.Select(a => (beforeByKey.GetValueOrDefault(KeyOf(a)), (DiffLogTableInfo?)a)),
        };
        foreach (var (old, current) in pairs)
        {
            var row = current ?? old!;
            string? changed = null;
            if (kind == DiffKind.Update)
            {
                var columns = ChangedColumns(old, current!);
                if (columns.All(static c => _noise.Contains(c)))
                {
                    continue;
                }

                changed = Clip(string.Join(',', columns), 2048);
            }

            yield return new SysDiffLog
            {
                OperationId = operationId,
                Kind = kind == DiffKind.Update && IsSoftDeleted(old) == true && IsSoftDeleted(current) == false ? DiffKind.Restore : kind,
                EntityType = Clip(entityType, 256)!,
                TableName = Clip(row.TableName, 128),
                EntityId = Clip(KeyOf(row), 128),
                Before = old is null ? null : Json(old),
                After = current is null ? null : Json(current),
                ChangedFields = changed,
            };
        }
    }

    private static string KeyOf(DiffLogTableInfo row)
        => string.Join(',', row.Columns.Where(static c => c.IsPrimaryKey).Select(static c => Text(c.Value)));

    private static List<string> ChangedColumns(DiffLogTableInfo? old, DiffLogTableInfo current)
    {
        var previous = old?.Columns.ToDictionary(static c => c.ColumnName, static c => Text(c.Value), StringComparer.OrdinalIgnoreCase) ?? [];
        return current.Columns
            .Where(c => !previous.TryGetValue(c.ColumnName, out var value) || !string.Equals(value, Text(c.Value), StringComparison.Ordinal))
            .Select(static c => c.ColumnName)
            .ToList();
    }

    /// <summary>
    /// 这一行的软删除标记。
    /// </summary>
    /// <remarks>
    /// 没有这一列为空；布尔、0 / 1 与文本都认（各库读回的类型不同）。
    /// </remarks>
    private static bool? IsSoftDeleted(DiffLogTableInfo? row)
    {
        var column = row?.Columns.FirstOrDefault(static c => string.Equals(c.ColumnName, SoftDeleteColumn, StringComparison.OrdinalIgnoreCase));
        return column?.Value switch
        {
            null or DBNull => null,
            bool flag => flag,
            int or long or short or byte => Convert.ToInt64(column.Value, CultureInfo.InvariantCulture) != 0,
            string text when bool.TryParse(text, out var parsed) => parsed,
            string text when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number != 0,
            _ => null,
        };
    }

    private static bool IsSensitive(string column) => _sensitive.Any(word => column.Contains(word, StringComparison.OrdinalIgnoreCase));

    private static string Json(DiffLogTableInfo row)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var column in row.Columns)
            {
                writer.WritePropertyName(column.ColumnName);
                if (IsSensitive(column.ColumnName) && column.Value is not null)
                {
                    writer.WriteStringValue(Masked);
                    continue;
                }

                switch (column.Value)
                {
                    case null:
                    case DBNull:
                        writer.WriteNullValue();
                        break;
                    case bool flag:
                        writer.WriteBooleanValue(flag);
                        break;
                    case int or long or short or byte:
                        writer.WriteNumberValue(Convert.ToInt64(column.Value, CultureInfo.InvariantCulture));
                        break;
                    case decimal number:
                        writer.WriteNumberValue(number);
                        break;
                    case double or float:
                        writer.WriteNumberValue(Convert.ToDouble(column.Value, CultureInfo.InvariantCulture));
                        break;
                    default:
                        writer.WriteStringValue(Text(column.Value));
                        break;
                }
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Text(object? value) => value switch
    {
        null or DBNull => string.Empty,
        DateTime time => time.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset at => at.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string? Clip(string? value, int max) => value is null || value.Length <= max ? value : value[..max];
}
