// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 访问策略快照：全部层启用的策略，按层与求值时机分好、按优先级排好（不可变）
/// </summary>
public sealed class AccessPolicySnapshot
{
    private readonly IReadOnlyDictionary<long, IReadOnlyList<CompiledAccessPolicy>> _tenants;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="policies">全部层启用的策略</param>
    /// <param name="version">构建时的版本号</param>
    public AccessPolicySnapshot(IEnumerable<CompiledAccessPolicy> policies, string? version)
    {
        ArgumentNullException.ThrowIfNull(policies);
        var ordered = policies
            .OrderBy(policy => policy.Priority)
            .ThenBy(policy => policy.PolicyId)
            .ToList();
        Platform = [.. ordered.Where(policy => policy.TenantId == 0)];
        PlatformBeforeAuthentication = [.. Platform.Where(policy => policy.IsBeforeAuthentication)];
        PlatformAfterAuthentication = [.. Platform.Where(policy => !policy.IsBeforeAuthentication)];
        _tenants = ordered
            .Where(policy => policy.TenantId > 0)
            .GroupBy(policy => policy.TenantId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<CompiledAccessPolicy>)[.. group]);
        Version = version;
    }

    /// <summary>
    /// 空快照
    /// </summary>
    public static AccessPolicySnapshot Empty { get; } = new([], null);

    /// <summary>
    /// 构建时的版本号
    /// </summary>
    public string? Version { get; }

    /// <summary>
    /// 平台级策略
    /// </summary>
    public IReadOnlyList<CompiledAccessPolicy> Platform { get; }

    /// <summary>
    /// 平台级、认证前就能求值的策略
    /// </summary>
    public IReadOnlyList<CompiledAccessPolicy> PlatformBeforeAuthentication { get; }

    /// <summary>
    /// 平台级、要等认证之后求值的策略
    /// </summary>
    public IReadOnlyList<CompiledAccessPolicy> PlatformAfterAuthentication { get; }

    /// <summary>
    /// 某个租户的策略
    /// </summary>
    public IReadOnlyList<CompiledAccessPolicy> GetTenant(long tenantId)
    {
        return _tenants.TryGetValue(tenantId, out var policies) ? policies : [];
    }
}
