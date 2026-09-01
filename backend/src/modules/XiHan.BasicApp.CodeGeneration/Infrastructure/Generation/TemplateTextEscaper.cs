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
    /// 转义为 HTML/Vue 模板的文本节点内容
    /// </summary>
    /// <param name="value">原始文本</param>
    /// <returns>在 <see cref="XmlDoc"/> 之上把花括号转成实体的单行文本</returns>
    /// <remarks>
    /// 文本节点位置比属性位置多一层风险：Vue 把 <c>{{ }}</c> 当插值。
    /// 列注释里出现半个 <c>{{</c> 会让 SFC 直接解析失败，出现完整的 <c>{{ x }}</c>
    /// 则被当成表达式求值、编译期报标识符不存在。转成实体后 Vue 按字面文本渲染。
    /// </remarks>
    public static string HtmlText(string? value)
    {
        return XmlDoc(value)
            .Replace("{", "&#123;", StringComparison.Ordinal)
            .Replace("}", "&#125;", StringComparison.Ordinal);
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
    /// 转义为 vue-i18n 消息文本（置于 TS 单引号字面量内）
    /// </summary>
    /// <param name="value">原始文本（表注释、列注释）</param>
    /// <returns>
    /// 反斜杠与单引号按 TS 字面量转义；<c>@</c> <c>|</c> <c>{</c> <c>}</c> 折成
    /// vue-i18n 字面量插值的单行文本
    /// </returns>
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
            switch (ch)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\'':
                    builder.Append("\\'");
                    break;
                // @ 链接消息、| 复数分隔、{} 具名插值：均为 vue-i18n 保留字符，
                // 单趟逐字符处理，避免顺序替换把自己产出的括号再转义一次
                case '@':
                case '|':
                case '{':
                case '}':
                    builder.Append("{\\'").Append(ch).Append("\\'}");
                    break;
                default:
                    builder.Append(ch);
                    break;
            }
        }

        return builder.ToString();
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
    /// 常量选择器候选项 → JS 数组字面量（形状不合规时 fail-closed）
    /// </summary>
    /// <param name="json">候选项 JSON（列配置里的自由文本）</param>
    /// <param name="columnLabel">列注释，仅用于报错定位</param>
    /// <param name="tsType">该列的 TS 类型，用于校验候选项值的类型是否对得上</param>
    /// <returns>形如 <c>[{ label: '甲', value: 'a' }]</c> 的字面量</returns>
    /// <remarks>
    /// 候选项是界面上的自由文本，填错的形式远多于填对的。原样吐进产物的话，
    /// 轻则渲成 <c>const xOptions = 日用,生鲜</c> 这种语法错，重则渲出能解析但类型对不上的值，
    /// 用户拿到的是一份编译不过的代码而非一条可读的错误。此处在生成期就拦下。
    /// </remarks>
    public static string SelectOptions(string? json, string? columnLabel, string? tsType)
    {
        var where = $"列「{(string.IsNullOrWhiteSpace(columnLabel) ? "(无注释)" : columnLabel)}」的";
        if (string.IsNullOrWhiteSpace(json))
        {
            // 不能回退成空数组：产出的是一个「必填却一个选项都没有」的下拉，表单从此提交不了，
            // 而且空数组字面量在 strict 下还会被推断成 any[]
            throw new InvalidOperationException($"{where}选择器没有候选项，请在列配置里填写或改掉选择器类型。");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{where}常量候选项不是合法 JSON：{Preview(json)}（{ex.Message}）");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"{where}常量候选项必须是数组：{Preview(json)}");
            }

            var count = 0;
            foreach (var item in document.RootElement.EnumerateArray())
            {
                count++;
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidOperationException(
                        $"{where}常量候选项每一项都要形如 {{\"label\": \"文本\", \"value\": \"文本或数字\"}}：{Preview(json)}");
                }

                // 同名键在 JSON 里合法，写成 JS 对象字面量却是 TS1117
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in item.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                    {
                        throw new InvalidOperationException($"{where}常量候选项里有重复键 {property.Name}：{Preview(json)}");
                    }
                }

                if (!item.TryGetProperty("label", out var label) || label.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException($"{where}常量候选项缺少文本 label：{Preview(json)}");
                }

                if (!item.TryGetProperty("value", out var value)
                    || value.ValueKind is not (JsonValueKind.String or JsonValueKind.Number))
                {
                    throw new InvalidOperationException($"{where}常量候选项的 value 只能是文本或数字：{Preview(json)}");
                }

                // 候选项的值会被下拉原样回抛进表单字段，类型对不上时提交必被后端拒
                var expected = value.ValueKind == JsonValueKind.Number ? "number" : "string";
                if (!string.IsNullOrEmpty(tsType) && tsType != expected)
                {
                    throw new InvalidOperationException(
                        $"{where}列类型是 {tsType}，候选项的 value 却是 {expected}：{Preview(json)}");
                }
            }

            if (count == 0)
            {
                throw new InvalidOperationException($"{where}选择器的候选项是空数组，请填写或改掉选择器类型。");
            }

            var builder = new StringBuilder(json.Length + 16);
            WriteJsValue(document.RootElement, builder);
            return builder.ToString();
        }
    }

    /// <summary>
    /// 报错用的内容摘要（过长截断，避免把整段 JSON 灌进错误信息）
    /// </summary>
    private static string Preview(string value)
    {
        var single = CollapseNewLines(value).Trim();
        return single.Length <= 120 ? single : single[..120] + "…";
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
