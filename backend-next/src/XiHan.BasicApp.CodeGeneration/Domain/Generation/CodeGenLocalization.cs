// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 生成的后端文案：每个区域一组键值
/// </summary>
/// <remarks>
/// 键与各包 <c>Localization/&lt;区域&gt;.json</c> 的约定一致（架构测试按它核对）：操作的 <c>&lt;操作&gt;.title</c> / <c>.description</c>、错误消息、
/// <c>openapi.tags.&lt;资源&gt;</c>、<c>permissions.&lt;权限码&gt;</c>；新建包另有 <c>openapi.groups.&lt;包&gt;</c>。连字符一律换成下划线
/// 「新建包」拼成 10 个 JSON 文件，「加入已有包」写进登记片段
/// </remarks>
public static class CodeGenLocalization
{
    private static readonly JsonWriterOptions _writerOptions = new() { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// 一个区域的键值，按登记顺序
    /// </summary>
    /// <param name="context">生成上下文</param>
    /// <param name="items">要登记的操作、权限码与错误码</param>
    /// <param name="locale">区域</param>
    /// <param name="includeGroup">是否带包的 OpenAPI 分组（新建包）</param>
    public static IReadOnlyList<KeyValuePair<string, string>> Entries(CodeGenContext context, CodeGenItems items, string locale, bool includeGroup)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(items);
        var name = ("name", context.DisplayName);
        var entries = new List<KeyValuePair<string, string>>();
        foreach (var operation in items.Operations)
        {
            var values = operation.Column is { } column
                ? new[] { name, ("label", CodeGenItems.LabelOf(column)), ("target", column.Relation?.Label ?? string.Empty) }
                : [name];
            entries.Add(new($"{operation.Key}.title", CodeGenPhrases.Text($"op.{Phrase(operation.Verb)}.title", locale, values)));
            entries.Add(new($"{operation.Key}.description", CodeGenPhrases.Text($"op.{Phrase(operation.Verb)}.description", locale, values)));
        }

        foreach (var message in items.Errors.SelectMany(static e => e.Messages))
        {
            entries.Add(new(message.Key, CodeGenPhrases.Text(message.Phrase, locale, [.. message.Values])));
        }

        var tag = context.Resource.Replace('-', '_');
        entries.Add(new($"openapi.tags.{tag}.name", context.DisplayName));
        entries.Add(new($"openapi.tags.{tag}.description", CodeGenPhrases.Text("tag.description", locale, name)));
        if (includeGroup)
        {
            entries.Add(new($"openapi.groups.{context.ModuleCode}.name", context.ModuleName));
            entries.Add(new($"openapi.groups.{context.ModuleCode}.description", CodeGenPhrases.Text("group.description", locale, ("module", context.ModuleName))));
        }

        foreach (var permission in items.Permissions)
        {
            entries.Add(new($"permissions.{permission.Key}.name", CodeGenPhrases.Text($"perm.{permission.Action}.name", locale, name)));
            entries.Add(new($"permissions.{permission.Key}.description", CodeGenPhrases.Text($"perm.{permission.Action}.description", locale, name)));
        }

        return entries;
    }

    /// <summary>
    /// 一个区域的 JSON 文件内容
    /// </summary>
    /// <param name="entries">键值</param>
    public static string Json(IReadOnlyList<KeyValuePair<string, string>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, _writerOptions))
        {
            writer.WriteStartObject();
            foreach (var (key, value) in entries)
            {
                writer.WriteString(key, value);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray()).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static string Phrase(string verb) => verb.Replace('-', '_');
}
