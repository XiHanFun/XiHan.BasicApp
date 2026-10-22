// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.Framework.Authorization.Conditions;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 授权行条件集的编译：按（条件集键, 权限码, 指纹）编译一次，进程内有界缓存
/// </summary>
/// <remarks>
/// 条件改了指纹就变，自然重编；所绑权限码登记了条件资源时，编译带上它的数据行属性。
/// </remarks>
public sealed class SaasConditionSetCompiler : IDisposable
{
    private const int CacheCapacity = 10_000;

    private readonly IConditionCompiler _compiler;
    private readonly IPermissionResourceRegistry _resources;
    private readonly TimeZoneInfo _timeZone;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = CacheCapacity });

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasConditionSetCompiler(IConditionCompiler compiler, IPermissionResourceRegistry resources, IOptions<SaasConditionOptions> options)
    {
        _compiler = compiler;
        _resources = resources;
        _timeZone = options.Value.ResolveTimeZone();
    }

    /// <summary>
    /// 编译一个条件集
    /// </summary>
    public CompiledConditionSet Compile(AuthorizationConditionSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        return _cache.GetOrCreate($"{set.Key}|{set.PermissionCode}|{set.Fingerprint}", entry =>
        {
            entry.Size = 1;
            entry.SlidingExpiration = TimeSpan.FromHours(1);
            return CompileClauses(set.PermissionCode, set.Clauses);
        })!;
    }

    /// <summary>
    /// 不经缓存编译（写路径校验、试算用）
    /// </summary>
    public CompiledConditionSet CompileClauses(string permissionCode, IEnumerable<AuthorizationConditionClause> clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        var groups = clauses
            .GroupBy(clause => clause.Group)
            .OrderBy(group => group.Key)
            .Select(group => new ConditionGroup(
                [.. group.Select(clause => new ConditionClause(
                    clause.AttributeName,
                    (ConditionOperator)clause.Operator,
                    clause.Value,
                    clause.IsNegated,
                    clause.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)))],
                group.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .ToList();
        return _compiler.Compile(new ConditionSet(groups), new ConditionCompileOptions
        {
            TimeZone = _timeZone,
            AdditionalAttributes = _resources.TryGet(permissionCode, out var registration) ? registration.Attributes : []
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cache.Dispose();
    }
}
