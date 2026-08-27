// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// DTO 行投影器：按列定义将资源 DTO 反射投影为字符串行（应用 ValueMap label 映射）。
/// </summary>
/// <remarks>
/// - 字段键大小写不敏感匹配 DTO 公共属性（前端 camelCase ↔ 后端 PascalCase）。
/// - 枚举渲染为数值字符串，与前端选项 value 对齐，便于 ValueMap 命中得到 label。
/// - 日期时间统一 yyyy-MM-dd HH:mm:ss；布尔渲染 true/false（前端可用 ValueMap 映射为 是/否）。
/// </remarks>
public static class DtoRowProjector
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, string), PropertyInfo?> PropertyCache = new();

    /// <summary>
    /// 按列投影
    /// </summary>
    /// <param name="dto">资源 DTO（不可为空）。</param>
    /// <param name="columns">列定义（不可为空）。</param>
    /// <returns>与列定义等长的字符串行。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="dto"/> 或 <paramref name="columns"/> 为 null。</exception>
    public static IReadOnlyList<string> Project(object dto, IReadOnlyList<ExportColumnDto> columns)
    {
        // 与同层导出写入器、全部映射器同一口径：null 入参当场抛 ArgumentNullException，
        // 而不是让 dto.GetType() / columns.Count 抛出无从定位的 NullReferenceException
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(columns);

        var type = dto.GetType();
        var row = new string[columns.Count];
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var property = ResolveProperty(type, column.Key);
            var raw = property?.GetValue(dto);
            row[i] = Render(raw, column);
        }
        return row;
    }

    private static PropertyInfo? ResolveProperty(Type type, string key)
    {
        return PropertyCache.GetOrAdd((type, key), static tuple =>
            tuple.Item1.GetProperty(tuple.Item2, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    private static string Render(object? raw, ExportColumnDto column)
    {
        if (raw is null)
        {
            return string.Empty;
        }

        var text = FormatValue(raw);
        if (column.ValueMap is { Count: > 0 } && column.ValueMap.TryGetValue(text, out var label))
        {
            return label;
        }
        return text;
    }

    private static string FormatValue(object raw)
    {
        return raw switch
        {
            string s => s,
            bool b => b ? "true" : "false",
            DateTimeOffset dto => dto.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            // 枚举渲染为名称（与前端 JsonStringEnumConverter 一致），匹配列快照 valueMap 的键得到 label
            Enum e => e.ToString(),
            Guid g => g.ToString(),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            IEnumerable enumerable => string.Join(",", enumerable.Cast<object?>().Select(item => item?.ToString() ?? string.Empty)),
            _ => SerializeComplex(raw)
        };
    }

    private static string SerializeComplex(object raw)
    {
        try
        {
            return JsonSerializer.Serialize(raw);
        }
        catch
        {
            return raw.ToString() ?? string.Empty;
        }
    }
}
