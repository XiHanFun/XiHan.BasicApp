// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;

namespace XiHan.BasicApp.Runtime.MultiTenancy;

/// <summary>
/// 租户是否有自己的库。
/// </summary>
/// <remarks>
/// 后台工作（日志写入、清理作业）自己开工作单元时不经管道、拿不到分片：有独立库的租户要改经 <c>ITenantScopeRunner</c> 进入该租户。
/// 解析失败也当作有（交给管道去失败，不悄悄写进平台库）。
/// </remarks>
public static class TenantShards
{
    /// <summary>
    /// <paramref name="tenantId"/> 有没有独立库。
    /// </summary>
    public static async ValueTask<bool> HasShardAsync(IServiceProvider services, string? tenantId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrEmpty(tenantId) || string.Equals(tenantId, TenantIds.Platform, StringComparison.Ordinal) || services.GetService<ITenantShardResolver>() is not { } resolver)
        {
            return false;
        }

        var resolved = await resolver.ResolveAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return !resolved.TryGetValue(out var shard) || shard.HasValue;
    }
}
