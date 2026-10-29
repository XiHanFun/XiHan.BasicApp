// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.MultiTenancy;

/// <summary>
/// 租户配额（Tenancy 实现，4.x 同）：席位与存储空间的有效上限取租户自己的，没有取套餐的，都没有即不限。
/// 只约束租户一侧的调用：平台主体代租户办事（开通、建用户、加成员）不受限，已经超额的租户只报告不阻止既有数据。
/// </summary>
public interface ITenantQuota
{
    /// <summary>
    /// 再占用 <paramref name="additionalBytes"/> 字节存储是否超额；超额时返回冲突（说明已用、上限与这次需要的大小）。
    /// </summary>
    /// <param name="context">调用上下文（平台主体的调用不受限）。</param>
    /// <param name="tenantId">占用存储的租户。</param>
    /// <param name="additionalBytes">这次新增的字节数。</param>
    /// <returns>不超额为成功。</returns>
    Task<Result> EnsureStorageAsync(InvocationContext context, string tenantId, long additionalBytes);

    /// <summary>
    /// 再占用 <paramref name="additionalSeats"/> 个席位（注册地是该租户的用户与租户成员）是否超额；超额时返回冲突。
    /// </summary>
    /// <param name="context">调用上下文（平台主体的调用不受限）。</param>
    /// <param name="tenantId">占用席位的租户。</param>
    /// <param name="additionalSeats">这次新增的席位数。</param>
    /// <returns>不超额为成功。</returns>
    Task<Result> EnsureSeatsAsync(InvocationContext context, string tenantId, int additionalSeats);
}
