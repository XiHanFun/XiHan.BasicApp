// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Text.Json;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 人工改过的字段集合
/// </summary>
/// <remarks>
/// 表与列配置保存时把值变了的被跟踪字段并入集合（JSON 数组），同步结构时这些字段保留人工值、其余跟随新的推断
/// 解析不了的旧值按空集合处理，等于全部跟随推断
/// </remarks>
public static class CodeGenModifiedFields
{
    /// <summary>
    /// 解析集合
    /// </summary>
    /// <param name="json">JSON 数组</param>
    public static HashSet<string> Parse(string? json)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return set;
        }

        try
        {
            foreach (var name in JsonSerializer.Deserialize(json, CodeGenerationJsonContext.Default.StringArray) ?? [])
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    set.Add(name.Trim());
                }
            }
        }
        catch (JsonException)
        {
            // 脏数据当作空集合，退化为全部跟随重新推断
        }

        return set;
    }

    /// <summary>
    /// 某个字段是否人工改过
    /// </summary>
    /// <param name="json">JSON 数组</param>
    /// <param name="field">字段名</param>
    public static bool Contains(string? json, string field) => Parse(json).Contains(field);

    /// <summary>
    /// 记下改之前的值
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="fields">被跟踪的字段</param>
    public static IReadOnlyDictionary<string, object?> Snapshot(object entity, IReadOnlyList<string> fields)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(fields);
        var type = entity.GetType();
        return fields.ToDictionary(static f => f, f => type.GetProperty(f, BindingFlags.Public | BindingFlags.Instance)?.GetValue(entity), StringComparer.Ordinal);
    }

    /// <summary>
    /// 把值变了的字段并入集合
    /// </summary>
    /// <param name="existing">原集合</param>
    /// <param name="entity">改之后的实体</param>
    /// <param name="before">改之前的值</param>
    /// <returns>新的集合；没有变化即原值</returns>
    public static string? Merge(string? existing, object entity, IReadOnlyDictionary<string, object?> before)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(before);
        var type = entity.GetType();
        var set = Parse(existing);
        var count = set.Count;
        foreach (var (field, old) in before)
        {
            if (!Equals(old, type.GetProperty(field, BindingFlags.Public | BindingFlags.Instance)?.GetValue(entity)))
            {
                set.Add(field);
            }
        }

        return set.Count == count ? existing : JsonSerializer.Serialize([.. set.Order(StringComparer.Ordinal)], CodeGenerationJsonContext.Default.StringArray);
    }
}
