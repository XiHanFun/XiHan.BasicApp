#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserModifiedFieldSet
// Guid:c0de9e00-0015-4a00-9000-000000000015
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/21 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Reflection;
using System.Text.Json;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// "已人工修改字段"集合的解析与合并（存储为 JSON 字符串数组）
/// </summary>
/// <remarks>
/// dirty-tracking 的单一事实源：更新配置时把值发生变化的字段并入集合；同步表结构时，
/// 集合内字段冻结（保留人工值）、集合外字段跟随最新表结构重新推断。字段名用实体属性名。
/// </remarks>
public static class UserModifiedFieldSet
{
    /// <summary>
    /// 解析 JSON 为字段名集合（大小写不敏感；非法/空返回空集）
    /// </summary>
    public static HashSet<string> Parse(string? json)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(json))
        {
            return set;
        }

        try
        {
            var names = JsonSerializer.Deserialize<string[]>(json);
            if (names is not null)
            {
                foreach (var name in names)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        set.Add(name.Trim());
                    }
                }
            }
        }
        catch (JsonException)
        {
            // 脏数据当作空集：dirty-tracking 退化为"全部跟随重新推断"，是安全的降级
        }

        return set;
    }

    /// <summary>
    /// 序列化为 JSON（空集返回 null，避免落库空数组）
    /// </summary>
    public static string? Serialize(IReadOnlyCollection<string> fields)
    {
        return fields.Count == 0 ? null : JsonSerializer.Serialize(fields.OrderBy(name => name, StringComparer.Ordinal));
    }

    /// <summary>
    /// 是否已标记为人工修改
    /// </summary>
    public static bool Contains(string? json, string fieldName)
    {
        return Parse(json).Contains(fieldName);
    }

    /// <summary>
    /// 把新变化的字段并入已有集合，返回新的 JSON（无变化则返回原值）
    /// </summary>
    public static string? Merge(string? existingJson, IEnumerable<string> changedFields)
    {
        var set = Parse(existingJson);
        var before = set.Count;
        foreach (var field in changedFields)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                set.Add(field.Trim());
            }
        }

        return set.Count == before ? existingJson : Serialize(set);
    }

    /// <summary>
    /// 从集合移除一个字段（"恢复自动推断"），返回新的 JSON
    /// </summary>
    public static string? Remove(string? existingJson, string fieldName)
    {
        var set = Parse(existingJson);
        return set.Remove(fieldName) ? Serialize(set) : existingJson;
    }

    /// <summary>
    /// 反射快照实体指定属性的当前值（供更新前后 diff）
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Snapshot(object entity, IReadOnlyList<string> propertyNames)
    {
        var snapshot = new Dictionary<string, object?>(propertyNames.Count, StringComparer.Ordinal);
        var type = entity.GetType();
        foreach (var name in propertyNames)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            snapshot[name] = property?.GetValue(entity);
        }

        return snapshot;
    }

    /// <summary>
    /// 与快照比对，返回值发生变化的属性名（相对快照的变化即视为人工修改）
    /// </summary>
    public static IReadOnlyList<string> DiffChanged(object entity, IReadOnlyDictionary<string, object?> before)
    {
        var changed = new List<string>();
        var type = entity.GetType();
        foreach (var (name, oldValue) in before)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            var newValue = property?.GetValue(entity);
            if (!Equals(oldValue, newValue))
            {
                changed.Add(name);
            }
        }

        return changed;
    }
}
