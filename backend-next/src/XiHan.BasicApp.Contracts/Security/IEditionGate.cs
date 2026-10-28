// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 套餐门控（方案 5.9）：租户能生效的权限白名单，有效权限与它求交。由 Tenancy 实现、Access 在 <c>auth.permissions</c>、权限目录与权限判定时消费；没有任何实现时不门控。
/// 平台侧不门控（调用方不该为平台调用它）；租户未绑定套餐即不受套餐限制（返回 <see langword="null"/>）。
/// </summary>
public interface IEditionGate
{
    /// <summary>
    /// 租户 <paramref name="tenantId"/> 的权限白名单；<see langword="null"/> 表示不受套餐限制，空集表示什么都不放行。
    /// </summary>
    /// <param name="tenantId">租户 ID（目录键）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>白名单（权限码，区分大小写），或 <see langword="null"/>。</returns>
    ValueTask<IReadOnlySet<string>?> AllowedAsync(string tenantId, CancellationToken cancellationToken);
}
