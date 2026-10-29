// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 导出的一列：字段、表头与原值到文字的映射。
/// </summary>
/// <remarks>
/// 4.x 同，来自页面字段：取行里的哪个字段、表头文字，以及把原值换成文字的映射（枚举、开关的显示名）。
/// </remarks>
/// <param name="Key">字段名（响应 JSON 里的属性名）。</param>
/// <param name="Title">表头。</param>
/// <param name="ValueMap">原值到显示文字的映射；没有为空。</param>
public sealed record ExportColumn(string Key, string Title, IReadOnlyDictionary<string, string>? ValueMap)
{
    /// <summary>
    /// 解析列快照。
    /// </summary>
    /// <remarks>
    /// 列快照形如 <c>[{ key, title, valueMap? }]</c>；格式不对的项跳过。
    /// </remarks>
    /// <param name="json">列快照。</param>
    /// <returns>列。</returns>
    public static IReadOnlyList<ExportColumn> Parse(string json)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(json) ? "[]" : json);
        var columns = new List<ExportColumn>();
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return columns;
        }

        foreach (var item in document.RootElement.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("key", out var key) || key.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var title = item.TryGetProperty("title", out var text) && text.ValueKind == JsonValueKind.String ? text.GetString()! : key.GetString()!;
            Dictionary<string, string>? map = null;
            if (item.TryGetProperty("valueMap", out var values) && values.ValueKind == JsonValueKind.Object)
            {
                map = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var pair in values.EnumerateObject())
                {
                    map[pair.Name] = pair.Value.ValueKind == JsonValueKind.String ? pair.Value.GetString()! : pair.Value.GetRawText();
                }
            }

            columns.Add(new ExportColumn(key.GetString()!, title, map));
        }

        return columns;
    }

    /// <summary>
    /// 取这一列在一行里的文字。
    /// </summary>
    /// <remarks>
    /// 字符串原样，数字与布尔取原文（有映射先映射），数组逐项拼接，空为空串。
    /// </remarks>
    /// <param name="row">一行（JSON 对象）。</param>
    /// <returns>文字。</returns>
    public string TextOf(JsonElement row)
    {
        if (row.ValueKind != JsonValueKind.Object || !row.TryGetProperty(Key, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind == JsonValueKind.Array
            ? string.Join(", ", value.EnumerateArray().Select(Map))
            : Map(value);
    }

    private string Map(JsonElement value)
    {
        var raw = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null or JsonValueKind.Undefined => string.Empty,
            _ => value.GetRawText(),
        };
        return ValueMap is not null && raw.Length > 0 && ValueMap.TryGetValue(raw, out var mapped) ? mapped : raw;
    }
}
