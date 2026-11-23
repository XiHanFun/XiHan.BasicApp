// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XiHan.BasicApp.Plugins.Domain.Packages;

/// <summary>
/// 插件的前端文案 <c>locales/&lt;区域&gt;.json</c>
/// </summary>
/// <remarks>
/// 与前端模块的文案同形：根上只有短名一个成员，下面按段嵌套对象，叶子是文本；读出来摊平成点号连接的键（<c>crm.customers.name</c>）
/// </remarks>
public static partial class PluginTexts
{
    /// <summary>
    /// 嵌套的层数上限
    /// </summary>
    private const int MaxDepth = 8;

    /// <summary>
    /// 一个区域的文案在包里的路径
    /// </summary>
    public static string EntryOf(string locale) => $"locales/{locale}.json";

    /// <summary>
    /// 读一份文案并摊平
    /// </summary>
    /// <param name="json">文案原文</param>
    /// <param name="name">插件短名：根上唯一的成员</param>
    /// <param name="texts">摊平后的键与文本</param>
    /// <param name="invalidKey">不合规则的第一个键（点号连接的路径）；整份读不出为 <c>$</c></param>
    /// <returns>是否合规</returns>
    public static bool TryRead(string json, string name, [NotNullWhen(true)] out IReadOnlyDictionary<string, string>? texts, [NotNullWhen(false)] out string? invalidKey)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(name);
        texts = null;
        invalidKey = "$";
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = MaxDepth + 1 });
        }
        catch (JsonException)
        {
            return false;
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            var flat = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var member in document.RootElement.EnumerateObject())
            {
                if (member.Name != name || member.Value.ValueKind != JsonValueKind.Object)
                {
                    invalidKey = member.Name;
                    return false;
                }

                if (Flatten(member.Value, member.Name, flat) is { } bad)
                {
                    invalidKey = bad;
                    return false;
                }
            }

            texts = flat;
            invalidKey = null;
            return true;
        }
    }

    /// <summary>
    /// 把一层对象的成员摊进结果，返回不合规则的第一个键
    /// </summary>
    private static string? Flatten(JsonElement element, string prefix, Dictionary<string, string> flat)
    {
        foreach (var member in element.EnumerateObject())
        {
            var key = prefix + "." + member.Name;
            if (!SegmentPattern().IsMatch(member.Name))
            {
                return key;
            }

            switch (member.Value.ValueKind)
            {
                case JsonValueKind.String:
                    flat[key] = member.Value.GetString()!;
                    break;
                case JsonValueKind.Object:
                    if (Flatten(member.Value, key, flat) is { } bad)
                    {
                        return bad;
                    }

                    break;
                default:
                    return key;
            }
        }

        return null;
    }

    /// <summary>
    /// 短名之下每一段的文法：与前端模块文案的键一样是小写下划线
    /// </summary>
    [GeneratedRegex("^[a-z][a-z0-9_]{0,63}$")]
    private static partial Regex SegmentPattern();
}
