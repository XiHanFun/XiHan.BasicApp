// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.MultiTenancy.Abstractions.Sharding;
using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.MultiTenancy;

/// <summary>
/// 后台逐侧遍历的范围
/// </summary>
/// <remarks>
/// 平台、共库的租户与已就绪的独立库。配了连接还没初始化的独立库不算：库可能还不存在，里面也不会有数据，算进去只会每次都失败
/// 停用、到期的租户照样算（清理与日志写入不看租户是否启用）。遍历独立库之前要先经框架的 <see cref="TenantShardScope"/> 绑定分片
/// </remarks>
/// <param name="tenants">租户目录</param>
public sealed class TenantSides(ITenantStore tenants)
{
    /// <summary>
    /// 列出要遍历的各侧
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>平台在最前，其后是租户 ID</returns>
    public async Task<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken)
    {
        var sides = new List<string> { TenantIds.Platform };
        sides.AddRange((await tenants.ListAsync(cancellationToken).ConfigureAwait(false)).Where(IsReachable).Select(static t => t.Id));
        return sides;
    }

    /// <summary>
    /// 这次遍历的结果
    /// </summary>
    /// <remarks>
    /// 有一处失败整次就算失败：作业页要看得出来，各处的完整异常已经进了日志
    /// </remarks>
    /// <param name="job">作业名</param>
    /// <param name="failures">失败的处数</param>
    /// <returns>没有失败为成功</returns>
    public static Result Outcome(string job, int failures)
        => failures == 0
            ? Result.Success()
            : Error.Unavailable(RuntimeErrors.SweepIncomplete, RuntimeMessages.SweepIncomplete, MessageArguments.Of(("job", job), ("failures", failures)));

    private static bool IsReachable(TenantInfo tenant)
        => !tenant.Properties.ContainsKey(TenantShardKeys.ConnectionString)
           || (tenant.Properties.TryGetValue(TenantSchemas.ReadyProperty, out var ready) && string.Equals(ready, "true", StringComparison.Ordinal));
}
