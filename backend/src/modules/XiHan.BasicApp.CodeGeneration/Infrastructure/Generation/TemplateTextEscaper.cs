// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Json;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 模板文本转义器（按产物中的宿主上下文转义表注释、列注释等自由文本）
/// </summary>
/// <remarks>
/// 表名、列注释等取自数据库元数据或用户输入，可能含引号、换行、尖括号。
/// 直插进 C# 字符串字面量 / XML 文档注释 / TS 字符串字面量 / HTML 属性，会让产物无法编译或解析。
/// 以 Scriban 过滤器形式暴露给模板：<c>{{ col.ColumnComment | cs_string }}</c>。
/// </remarks>
public static class TemplateTextEscaper
{
    /// <summary>
    /// 转义为 C# 双引号字符串字面量内容
    /// </summary>
    /// <param name="value">原始文本</param>
    /// <returns>可安全放进 <c>"…"</c> 的文本；换行折成 <c>\n</c> 转义序列</returns>
    public static string CSharpString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// 转义为 XML 文档注释内容
    /// </summary>
    /// <param name="value">原始文本</param>
    /// <returns>转义尖括号与 &amp; 的单行文本；换行折成空格，避免第二行丢失 /// 前缀</returns>
    public static string XmlDoc(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return CollapseNewLines(value)
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    /// <summary>
    /// 转义为 TypeScript 单引号字符串字面量内容
    /// </summary>
    /// <param name="value">原始文本</param>
    /// <returns>可安全放进 <c>'…'</c> 的单行文本</returns>
    public static string TsString(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return CollapseNewLines(value)
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal);
    }

    /// <summary>
    /// 转义为 HTML/Vue 双引号属性值内容
    /// </summary>
    /// <param name="value">原始文本</param>
    /// <returns>转义引号、尖括号与 &amp; 的单行文本</returns>
    public static string HtmlAttribute(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return CollapseNewLines(value)
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }

    /// <summary>
    /// 转义为块注释内容
    /// </summary>
    /// <param name="value">原始文本</param>
    /// <returns>拆开 <c>*/</c> 的单行文本，避免提前结束 <c>/* … */</c></returns>
    public static string BlockComment(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return CollapseNewLines(value).Replace("*/", "* /", StringComparison.Ordinal);
    }

    /// <summary>
    /// 转义为 HTML 注释内容
    /// </summary>
    /// <param name="value">原始文本</param>
    /// <returns>拆开 <c>--&gt;</c> 与 <c>--!&gt;</c> 的单行文本，避免提前结束 <c>&lt;!-- … --&gt;</c></returns>
    public static string HtmlComment(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return CollapseNewLines(value)
            .Replace("--!>", "- -!>", StringComparison.Ordinal)
            .Replace("-->", "- ->", StringComparison.Ordinal);
    }

    /// <summary>
    /// JSON 文本重排为 JS 对象字面量
    /// </summary>
    /// <param name="json">JSON 文本（如常量选择器的候选项）</param>
    /// <returns>单引号字符串、键按需去引号、大括号内留空格的字面量；解析失败时原样返回</returns>
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
    /// 递归写出 JS 字面量
    /// </summary>
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

    /// <summary>
    /// 是否为可省略引号的 JS 标识符
    /// </summary>
    private static bool IsJsIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name) || (!char.IsLetter(name[0]) && name[0] is not ('_' or '$')))
        {
            return false;
        }

        return name.All(ch => char.IsLetterOrDigit(ch) || ch is '_' or '$');
    }

    /// <summary>
    /// 换行折成单个空格并去除首尾空白
    /// </summary>
    private static string CollapseNewLines(string value)
    {
        return value
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
    }
}
