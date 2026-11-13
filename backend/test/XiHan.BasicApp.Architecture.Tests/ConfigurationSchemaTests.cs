// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Architecture.Tests.Model;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查宿主配置与它的 JSON Schema 同步
/// </summary>
/// <remarks>
/// <c>ConfigurationSchema.json</c> 给编辑器做 <c>appsettings*.json</c> 的补全与校验。宿主配置文件里 <c>XiHan</c>、<c>BasicApp</c> 下写出的每个键都要在 Schema 里，
/// 加了配置项却没补 Schema 即失败；注释掉的可选键不在检查范围
/// </remarks>
public sealed class ConfigurationSchemaTests
{
    private static readonly JsonDocumentOptions _jsonc = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    private static readonly string[] _sections = ["XiHan", "BasicApp"];

    public static TheoryData<string> SettingsFiles => ["appsettings.json", "appsettings.Development.json"];

    [Theory]
    [MemberData(nameof(SettingsFiles))]
    public void EveryKey_IsDescribedByTheSchema(string fileName)
    {
        var host = Path.Combine(Packages.SourceRoot, "XiHan.BasicApp.Host");
        using var schema = JsonDocument.Parse(File.ReadAllText(Path.Combine(host, "ConfigurationSchema.json")));
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(host, fileName)), _jsonc);

        var missing = new List<string>();
        foreach (var section in _sections)
        {
            if (settings.RootElement.TryGetProperty(section, out var value))
            {
                Collect(schema.RootElement, value, Child(schema.RootElement, schema.RootElement, section), section, missing);
            }
        }

        Assert.True(missing.Count == 0, $"{fileName} has keys that ConfigurationSchema.json does not describe:{Environment.NewLine}{string.Join(Environment.NewLine, missing)}");
    }

    private static void Collect(JsonElement root, JsonElement value, JsonElement? node, string path, List<string> missing)
    {
        if (node is not { } described)
        {
            missing.Add(path);
            return;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in value.EnumerateObject())
                {
                    Collect(root, property.Value, Child(root, described, property.Name), $"{path}:{property.Name}", missing);
                }

                break;
            case JsonValueKind.Array:
                var items = Resolve(root, described).TryGetProperty("items", out var item) ? item : (JsonElement?)null;
                var index = 0;
                foreach (var element in value.EnumerateArray())
                {
                    Collect(root, element, items, $"{path}:{index++}", missing);
                }

                break;
        }
    }

    /// <summary>
    /// 子键在 Schema 里的描述
    /// </summary>
    /// <remarks>
    /// 先找 properties，再找字典的 additionalProperties
    /// </remarks>
    private static JsonElement? Child(JsonElement root, JsonElement node, string name)
    {
        var resolved = Resolve(root, node);
        if (resolved.TryGetProperty("properties", out var properties) && properties.TryGetProperty(name, out var property))
        {
            return property;
        }

        return resolved.TryGetProperty("additionalProperties", out var dictionary) && dictionary.ValueKind == JsonValueKind.Object ? dictionary : null;
    }

    /// <summary>
    /// 顺着本文件内的 $ref 找到定义
    /// </summary>
    private static JsonElement Resolve(JsonElement root, JsonElement node)
    {
        if (!node.TryGetProperty("$ref", out var reference) || reference.GetString() is not { } pointer || !pointer.StartsWith("#/", StringComparison.Ordinal))
        {
            return node;
        }

        var target = root;
        foreach (var segment in pointer[2..].Split('/'))
        {
            target = target.GetProperty(segment);
        }

        return target;
    }
}
