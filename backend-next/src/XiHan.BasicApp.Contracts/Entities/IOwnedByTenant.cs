// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 平台库里带「属主」的实体。
/// </summary>
/// <remarks>
/// 不实现 <c>IMultiTenant</c>（不受框架仓储的租户过滤），用显式列区分平台的还是某租户的。
/// 读共享（平台 + 当前租户）与写不共享（只当前租户）的条件由 <c>Runtime</c> 的查询助手施加。
/// </remarks>
public interface IOwnedByTenant
{
    /// <summary>
    /// 属主租户 ID。
    /// </summary>
    /// <remarks>
    /// 平台的记录固定为 <see cref="TenantIds.Platform"/>。
    /// </remarks>
    string OwnerTenantId { get; set; }
}
