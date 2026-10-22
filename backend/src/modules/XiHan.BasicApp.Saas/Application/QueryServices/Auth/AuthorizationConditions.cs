// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 授权快照里的权限条件：有条件授予的路径、用户有条件的禁止，以及它们引用的条件集
/// </summary>
/// <remarks>
/// 只收需要求值的权限：有无条件授予路径的权限不进 <see cref="Grants"/>。
/// 有条件授予的权限照样在快照的权限码里，菜单、按钮与「我的权限」按已授予显示，接口上再求条件。
/// 可序列化，随授权快照进分布式缓存。
/// </remarks>
public sealed class AuthorizationConditions
{
    /// <summary>
    /// 权限码 → 授予路径（任一路径成立即授予）
    /// </summary>
    public Dictionary<string, List<ConditionalGrantPath>> Grants { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 权限码 → 用户直授上有条件禁止的条件集键（任一命中即拒绝）
    /// </summary>
    public Dictionary<string, List<string>> Denies { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 条件集键 → 条件集
    /// </summary>
    public Dictionary<string, AuthorizationConditionSet> Sets { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    /// 没有任何需要求值的条件
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty => Grants.Count == 0 && Denies.Count == 0;

    /// <summary>
    /// 该权限码是否要求值
    /// </summary>
    public bool Requires(string permissionCode)
    {
        return Grants.ContainsKey(permissionCode) || Denies.ContainsKey(permissionCode);
    }

    /// <summary>
    /// 只留给定权限码的条件（套餐门控收窄后用）
    /// </summary>
    public AuthorizationConditions Retain(IEnumerable<string> permissionCodes)
    {
        var codes = permissionCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var retained = new AuthorizationConditions();
        foreach (var (code, paths) in Grants.Where(item => codes.Contains(item.Key)))
        {
            retained.Grants[code] = paths;
        }

        foreach (var (code, keys) in Denies.Where(item => codes.Contains(item.Key)))
        {
            retained.Denies[code] = keys;
        }

        var usedKeys = retained.Grants.Values
            .SelectMany(paths => paths)
            .SelectMany(path => path.GrantSetKeys.Concat(path.DenySetKeys))
            .Concat(retained.Denies.Values.SelectMany(keys => keys))
            .ToHashSet(StringComparer.Ordinal);
        foreach (var key in usedKeys)
        {
            if (Sets.TryGetValue(key, out var set))
            {
                retained.Sets[key] = set;
            }
        }

        return retained;
    }

    /// <summary>
    /// 反序列化后重建字典比较器（权限码不区分大小写）
    /// </summary>
    public AuthorizationConditions Normalize()
    {
        return new AuthorizationConditions
        {
            Grants = new Dictionary<string, List<ConditionalGrantPath>>(Grants, StringComparer.OrdinalIgnoreCase),
            Denies = new Dictionary<string, List<string>>(Denies, StringComparer.OrdinalIgnoreCase),
            Sets = new Dictionary<string, AuthorizationConditionSet>(Sets, StringComparer.Ordinal)
        };
    }
}

/// <summary>
/// 一条有条件的授予路径：一条角色链、一条用户直授或一条委托
/// </summary>
/// <remarks>
/// 成立 = （有无条件的授予行，或任一授予条件集命中）且 禁止条件集都不命中。
/// </remarks>
public sealed class ConditionalGrantPath
{
    /// <summary>
    /// 来源：<c>role:{角色主键}</c>、<c>user:{用户直授主键}</c>、<c>delegation-role:{角色主键}</c>
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 链上有无条件的授予行
    /// </summary>
    public bool HasUnconditionalGrant { get; set; }

    /// <summary>
    /// 授予行上的条件集键（任一命中即可）
    /// </summary>
    public List<string> GrantSetKeys { get; set; } = [];

    /// <summary>
    /// 链上有条件的禁止行的条件集键（命中任一则本路径不成立）
    /// </summary>
    public List<string> DenySetKeys { get; set; } = [];
}

/// <summary>
/// 一条授权行上的条件集：组内且、组间或
/// </summary>
public sealed class AuthorizationConditionSet
{
    /// <summary>
    /// 键：<c>rp:{角色权限主键}</c> 或 <c>up:{用户直授主键}</c>
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 所绑权限码（编译时据此带上该权限码登记的数据行属性）
    /// </summary>
    public string PermissionCode { get; set; } = string.Empty;

    /// <summary>
    /// 指纹：条件行主键与修改时间，条件改了指纹就变，编译缓存随之失效
    /// </summary>
    public string Fingerprint { get; set; } = string.Empty;

    /// <summary>
    /// 子句（只含启用的）
    /// </summary>
    public List<AuthorizationConditionClause> Clauses { get; set; } = [];
}

/// <summary>
/// 条件子句原文
/// </summary>
public sealed class AuthorizationConditionClause
{
    /// <summary>
    /// 条件行主键
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 条件分组
    /// </summary>
    public int Group { get; set; }

    /// <summary>
    /// 属性名
    /// </summary>
    public string AttributeName { get; set; } = string.Empty;

    /// <summary>
    /// 运算符（与框架 ConditionOperator 的整数值一致）
    /// </summary>
    public int Operator { get; set; }

    /// <summary>
    /// 是否取反
    /// </summary>
    public bool IsNegated { get; set; }

    /// <summary>
    /// 条件值原文
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
