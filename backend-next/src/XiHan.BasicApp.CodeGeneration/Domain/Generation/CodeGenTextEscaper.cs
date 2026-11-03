// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Json;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 模板的转义过滤器
/// </summary>
/// <remarks>
/// 照 4.x：表注释、列注释是自由文本，直插产物会破坏宿主语法，模板按插值点所在的上下文选用（如 <c>cs_string</c>、<c>xml_doc</c>、<c>ts_string</c>）
/// 常量选项的形状不对时抛 <see cref="CodeGenRenderException"/>，按调用方区域出文案（4.x 是写死的中文）
/// </remarks>
public static class CodeGenTextEscaper
{
    private const int PreviewLength = 120;

    /// <summary>
    /// C# 双引号字符串字面量里的内容
    /// </summary>
    /// <param name="value">原文</param>
    public static string CSharpString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            _ = ch switch
            {
                '\\' => builder.Append("\\\\"),
                '"' => builder.Append("\\\""),
                '\r' => builder.Append("\\r"),
                '\n' => builder.Append("\\n"),
                '\t' => builder.Append("\\t"),
                _ => builder.Append(ch),
            };
        }

        return builder.ToString();
    }

    /// <summary>
    /// XML 文档注释里的内容
    /// </summary>
    /// <param name="value">原文</param>
    public static string XmlDoc(string? value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : CollapseNewLines(value)
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// HTML / Vue 模板文本节点里的内容
    /// </summary>
    /// <remarks>
    /// 比属性多一层：Vue 把双花括号当插值，花括号也转成实体
    /// </remarks>
    /// <param name="value">原文</param>
    public static string HtmlText(string? value)
        => XmlDoc(value)
            .Replace("{", "&#123;", StringComparison.Ordinal)
            .Replace("}", "&#125;", StringComparison.Ordinal);

    /// <summary>
    /// TypeScript 单引号字符串字面量里的内容
    /// </summary>
    /// <param name="value">原文</param>
    public static string TsString(string? value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : CollapseNewLines(value)
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("'", "\\'", StringComparison.Ordinal);

    /// <summary>
    /// vue-i18n 消息文本（放在 TypeScript 单引号字面量里）
    /// </summary>
    /// <remarks>
    /// <c>@</c>、<c>|</c>、花括号是 vue-i18n 的保留字符，折成字面量插值
    /// </remarks>
    /// <param name="value">原文</param>
    public static string I18nMessage(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var collapsed = CollapseNewLines(value);
        var builder = new StringBuilder(collapsed.Length + 16);
        foreach (var ch in collapsed)
        {
            _ = ch switch
            {
                '\\' => builder.Append("\\\\"),
                '\'' => builder.Append("\\'"),
                '@' or '|' or '{' or '}' => builder.Append("{\\'").Append(ch).Append("\\'}"),
                _ => builder.Append(ch),
            };
        }

        return builder.ToString();
    }

    /// <summary>
    /// HTML / Vue 双引号属性值里的内容
    /// </summary>
    /// <param name="value">原文</param>
    public static string HtmlAttribute(string? value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : CollapseNewLines(value)
                .Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("\"", "&quot;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace(">", "&gt;", StringComparison.Ordinal);

    /// <summary>
    /// 块注释里的内容
    /// </summary>
    /// <param name="value">原文</param>
    public static string BlockComment(string? value)
        => string.IsNullOrEmpty(value) ? string.Empty : CollapseNewLines(value).Replace("*/", "* /", StringComparison.Ordinal);

    /// <summary>
    /// HTML 注释里的内容
    /// </summary>
    /// <param name="value">原文</param>
    public static string HtmlComment(string? value)
        => string.IsNullOrEmpty(value)
            ? string.Empty
            : CollapseNewLines(value)
                .Replace("--!>", "- -!>", StringComparison.Ordinal)
                .Replace("-->", "- ->", StringComparison.Ordinal);

    /// <summary>
    /// JSON 文本重排为 JavaScript 字面量
    /// </summary>
    /// <param name="json">JSON 文本；空即空数组</param>
    public static string JsLiteral(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "[]";
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var builder = new StringBuilder(json.Length + 16);
            WriteJsValue(document.RootElement, builder);
            return builder.ToString();
        }
        catch (JsonException)
        {
            return json;
        }
    }

    /// <summary>
    /// 常量选项 → JavaScript 数组字面量
    /// </summary>
    /// <remarks>
    /// 照 4.x 在生成时拦下形状不对的选项（不是数组、不是 <c>{ label, value }</c>、重复键、空数组、值的类型与列不符），不产出编译不过的代码
    /// </remarks>
    /// <param name="json">常量选项 JSON</param>
    /// <param name="columnLabel">列的显示名（报错用）</param>
    /// <param name="tsType">列的 TypeScript 类型</param>
    /// <exception cref="CodeGenRenderException">选项缺失或形状不对</exception>
    public static string SelectOptions(string? json, string? columnLabel, string? tsType)
    {
        var column = string.IsNullOrWhiteSpace(columnLabel) ? "?" : columnLabel;
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new CodeGenRenderException(CodeGenerationMessages.ConstOptionsMissing, ("column", column));
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new CodeGenRenderException(CodeGenerationMessages.ConstOptionsInvalid, ("column", column), ("value", Preview(json)));
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
            {
                throw new CodeGenRenderException(CodeGenerationMessages.ConstOptionsInvalid, ("column", column), ("value", Preview(json)));
            }

            foreach (var item in document.RootElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object
                    || item.EnumerateObject().GroupBy(static p => p.Name, StringComparer.Ordinal).Any(static g => g.Count() > 1)
                    || !item.TryGetProperty("label", out var label) || label.ValueKind != JsonValueKind.String
                    || !item.TryGetProperty("value", out var value) || value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
                {
                    throw new CodeGenRenderException(CodeGenerationMessages.ConstOptionsInvalid, ("column", column), ("value", Preview(json)));
                }

                // 选中的值原样回到表单字段，类型对不上提交必被后端拒
                var valueType = value.ValueKind == JsonValueKind.Number ? "number" : "string";
                if (!string.IsNullOrEmpty(tsType) && tsType != valueType)
                {
                    throw new CodeGenRenderException(CodeGenerationMessages.ConstOptionsTypeMismatch, ("column", column), ("type", tsType), ("valueType", valueType));
                }
            }

            var builder = new StringBuilder(json.Length + 16);
            WriteJsValue(document.RootElement, builder);
            return builder.ToString();
        }
    }

    private static string Preview(string value)
    {
        var single = CollapseNewLines(value).Trim();
        return single.Length <= PreviewLength ? single : string.Concat(single.AsSpan(0, PreviewLength), "…");
    }

    private static void WriteJsValue(JsonElement element, StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                builder.Append('[');
                var firstItem = true;
                foreach (var item in element.EnumerateArray())
                {
                    if (!firstItem)
                    {
                        builder.Append(", ");
                    }

                    firstItem = false;
                    WriteJsValue(item, builder);
                }

                builder.Append(']');
                break;
            case JsonValueKind.Object:
                builder.Append("{ ");
                var firstProperty = true;
                foreach (var property in element.EnumerateObject())
                {
                    if (!firstProperty)
                    {
                        builder.Append(", ");
                    }

                    firstProperty = false;
                    builder.Append(IsJsIdentifier(property.Name) ? property.Name : $"'{TsString(property.Name)}'");
                    builder.Append(": ");
                    WriteJsValue(property.Value, builder);
                }

                builder.Append(" }");
                break;
            case JsonValueKind.String:
                builder.Append('\'').Append(TsString(element.GetString())).Append('\'');
                break;
            case JsonValueKind.Null:
                builder.Append("null");
                break;
            case JsonValueKind.Undefined:
                builder.Append("undefined");
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }

    private static bool IsJsIdentifier(string name)
        => !string.IsNullOrEmpty(name)
            && (char.IsLetter(name[0]) || name[0] is '_' or '$')
            && name.All(static ch => char.IsLetterOrDigit(ch) || ch is '_' or '$');

    private static string CollapseNewLines(string value)
        => value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
}
