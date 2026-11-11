// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.MultiTenancy.Abstractions.Tenants;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.MultiTenancy;

/// <summary>
/// 租户目录的整表读取
/// </summary>
public static class TenantStoreExtensions
{
    /// <summary>
    /// 逐页读完全部租户
    /// </summary>
    /// <remarks>
    /// 按最大页长翻页到续页令牌为空；任一页读失败即整次失败（目录不可达不当作没有租户）
    /// </remarks>
    /// <param name="store">租户目录</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按 ID 升序的全部租户</returns>
    public static async Task<Result<IReadOnlyList<TenantInfo>>> ListAllAsync(this ITenantStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(store);
        var tenants = new List<TenantInfo>();
        string? token = null;
        do
        {
            var page = await store.ListAsync(new TenantQuery { PageSize = TenantQuery.MaxPageSize, ContinuationToken = token }, cancellationToken).ConfigureAwait(false);
            if (!page.TryGetValue(out var value))
            {
                return page.Error!;
            }

            tenants.AddRange(value.Items);
            token = value.ContinuationToken;
        }
        while (token is not null);

        return tenants;
    }
}
