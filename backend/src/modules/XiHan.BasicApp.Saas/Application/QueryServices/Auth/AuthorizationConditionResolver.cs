// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 一条角色继承链：持有（或被委托）的角色加上它经启用角色可达的上级
/// </summary>
/// <param name="Source">来源：<c>role:{角色主键}</c> 或 <c>delegation-role:{角色主键}</c></param>
/// <param name="RoleIds">链上的角色主键</param>
public sealed record AuthorizationRoleChain(string Source, IReadOnlySet<long> RoleIds);

/// <summary>
/// 授权合并的输入：角色链与链上授权行、用户直授、直接委托的权限、授权行上启用的条件
/// </summary>
public sealed record AuthorizationGrantSources(
    IReadOnlyList<AuthorizationRoleChain> RoleChains,
    IReadOnlyList<SysRolePermission> RolePermissions,
    IReadOnlyList<SysUserPermission> UserPermissions,
    IReadOnlyCollection<long> DelegatedPermissionIds,
    IReadOnlyList<SysPermissionCondition> Conditions);

/// <summary>
/// 授权合并的结果（按权限主键）
/// </summary>
/// <param name="GrantedPermissionIds">已授予的权限：无条件授予的，加上只有有条件授予的</param>
/// <param name="ConditionalGrants">只有有条件授予路径的权限 → 路径</param>
/// <param name="ConditionalDenies">用户直授上有条件禁止的权限 → 条件集键（只收已授予的权限）</param>
/// <param name="Sets">路径与禁止引用到的条件集（权限码待调用方按目录补上）</param>
public sealed record AuthorizationGrantResolution(
    HashSet<long> GrantedPermissionIds,
    Dictionary<long, List<ConditionalGrantPath>> ConditionalGrants,
    Dictionary<long, List<string>> ConditionalDenies,
    Dictionary<string, (long PermissionId, AuthorizationConditionSet Set)> Sets);

/// <summary>
/// 授权合并：把角色链、用户直授与委托连同授权行上的条件合并成「无条件授予 + 有条件路径 + 有条件禁止」
/// </summary>
/// <remarks>
/// 条件写在授予行上是「满足才授予」，写在禁止行上是「满足才禁止」：
/// <list type="bullet">
/// <item>每条角色链、每条用户直授、每条委托各是一条授予路径；角色链对某权限成立 = 授予条件命中，且链上禁止条件都不命中</item>
/// <item>链上无条件的禁止照旧整条链不授予；有条件的禁止只在命中时扣除，且只作用于本链</item>
/// <item>任一路径无条件成立即无条件授予；否则只要有路径，就是有条件授予</item>
/// <item>用户直授的无条件禁止压过一切授予；有条件的禁止命中时拒绝</item>
/// <item>委托表没有条件，直接委托的权限是无条件路径；委托角色按该角色的链结算</item>
/// </list>
/// 没有启用条件的授权行就是无条件的（停用条件等于放宽）。
/// </remarks>
public static class AuthorizationConditionResolver
{
    /// <summary>
    /// 合并
    /// </summary>
    public static AuthorizationGrantResolution Resolve(AuthorizationGrantSources sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var conditionsByRolePermission = sources.Conditions
            .Where(condition => condition.RolePermissionId is > 0)
            .ToLookup(condition => condition.RolePermissionId!.Value);
        var conditionsByUserPermission = sources.Conditions
            .Where(condition => condition.UserPermissionId is > 0)
            .ToLookup(condition => condition.UserPermissionId!.Value);
        var sets = new Dictionary<string, (long PermissionId, AuthorizationConditionSet Set)>(StringComparer.Ordinal);

        string? RoleSetKey(SysRolePermission row)
        {
            return RegisterSet($"rp:{row.BasicId}", row.PermissionId, conditionsByRolePermission[row.BasicId]);
        }

        string? UserSetKey(SysUserPermission row)
        {
            return RegisterSet($"up:{row.BasicId}", row.PermissionId, conditionsByUserPermission[row.BasicId]);
        }

        string? RegisterSet(string key, long permissionId, IEnumerable<SysPermissionCondition> conditions)
        {
            var clauses = conditions.OrderBy(condition => condition.ConditionGroup).ThenBy(condition => condition.BasicId).ToList();
            if (clauses.Count == 0)
            {
                return null;
            }

            if (!sets.ContainsKey(key))
            {
                sets[key] = (permissionId, new AuthorizationConditionSet
                {
                    Key = key,
                    Fingerprint = string.Join(',', clauses.Select(clause => $"{clause.BasicId}@{(clause.ModifiedTime ?? clause.CreatedTime).UtcTicks}")),
                    Clauses = [.. clauses.Select(clause => new AuthorizationConditionClause
                    {
                        Id = clause.BasicId,
                        Group = clause.ConditionGroup,
                        AttributeName = clause.AttributeName,
                        Operator = (int)clause.Operator,
                        IsNegated = clause.IsNegated,
                        Value = clause.ConditionValue
                    })]
                });
            }

            return key;
        }

        var unconditional = new HashSet<long>();
        var paths = new Dictionary<long, List<ConditionalGrantPath>>();

        void AddPath(long permissionId, ConditionalGrantPath path)
        {
            if (!paths.TryGetValue(permissionId, out var list))
            {
                paths[permissionId] = list = [];
            }

            list.Add(path);
        }

        var grantsByRole = sources.RolePermissions
            .Where(row => row.PermissionAction == PermissionAction.Grant)
            .ToLookup(row => row.RoleId);
        var deniesByRole = sources.RolePermissions
            .Where(row => row.PermissionAction == PermissionAction.Deny)
            .ToLookup(row => row.RoleId);
        foreach (var chain in sources.RoleChains)
        {
            var chainDenies = chain.RoleIds.SelectMany(roleId => deniesByRole[roleId]).ToLookup(row => row.PermissionId);
            foreach (var grantRows in chain.RoleIds.SelectMany(roleId => grantsByRole[roleId]).GroupBy(row => row.PermissionId))
            {
                var denyKeys = new List<string>();
                var deniedOutright = false;
                foreach (var denyRow in chainDenies[grantRows.Key])
                {
                    if (RoleSetKey(denyRow) is { } denyKey)
                    {
                        denyKeys.Add(denyKey);
                    }
                    else
                    {
                        deniedOutright = true;
                    }
                }

                // 链上无条件的禁止：整条链对这个权限不授予（与原先「链上授予减链上禁止」一致）
                if (deniedOutright)
                {
                    continue;
                }

                var grantKeys = new List<string>();
                var hasUnconditionalGrant = false;
                foreach (var grantRow in grantRows)
                {
                    if (RoleSetKey(grantRow) is { } grantKey)
                    {
                        grantKeys.Add(grantKey);
                    }
                    else
                    {
                        hasUnconditionalGrant = true;
                    }
                }

                if (hasUnconditionalGrant && denyKeys.Count == 0)
                {
                    unconditional.Add(grantRows.Key);
                    continue;
                }

                AddPath(grantRows.Key, new ConditionalGrantPath
                {
                    Source = chain.Source,
                    HasUnconditionalGrant = hasUnconditionalGrant,
                    GrantSetKeys = [.. grantKeys.Distinct(StringComparer.Ordinal)],
                    DenySetKeys = [.. denyKeys.Distinct(StringComparer.Ordinal)]
                });
            }
        }

        var deniedOutrightIds = new HashSet<long>();
        var conditionalDenies = new Dictionary<long, List<string>>();
        foreach (var row in sources.UserPermissions)
        {
            var key = UserSetKey(row);
            if (row.PermissionAction == PermissionAction.Grant)
            {
                if (key is null)
                {
                    unconditional.Add(row.PermissionId);
                }
                else
                {
                    AddPath(row.PermissionId, new ConditionalGrantPath { Source = $"user:{row.BasicId}", GrantSetKeys = [key] });
                }

                continue;
            }

            if (key is null)
            {
                deniedOutrightIds.Add(row.PermissionId);
            }
            else
            {
                if (!conditionalDenies.TryGetValue(row.PermissionId, out var keys))
                {
                    conditionalDenies[row.PermissionId] = keys = [];
                }

                keys.Add(key);
            }
        }

        unconditional.UnionWith(sources.DelegatedPermissionIds);

        // 用户无条件的禁止压过一切授予
        var granted = unconditional.Union(paths.Keys).Where(id => !deniedOutrightIds.Contains(id)).ToHashSet();
        var conditionalGrants = paths
            .Where(item => granted.Contains(item.Key) && !unconditional.Contains(item.Key))
            .ToDictionary(item => item.Key, item => item.Value);
        var retainedDenies = conditionalDenies
            .Where(item => granted.Contains(item.Key))
            .ToDictionary(item => item.Key, item => item.Value);

        var usedKeys = conditionalGrants.Values
            .SelectMany(list => list)
            .SelectMany(path => path.GrantSetKeys.Concat(path.DenySetKeys))
            .Concat(retainedDenies.Values.SelectMany(keys => keys))
            .ToHashSet(StringComparer.Ordinal);
        var usedSets = sets
            .Where(item => usedKeys.Contains(item.Key))
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);

        return new AuthorizationGrantResolution(granted, conditionalGrants, retainedDenies, usedSets);
    }
}
