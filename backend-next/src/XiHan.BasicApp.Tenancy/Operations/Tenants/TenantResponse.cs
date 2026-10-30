// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// 租户的响应形态：列表项、创建 / 修改的响应与详情里的基本信息共用。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="TenantId">租户 ID（目录键，登录时填的租户）。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="Status">状态。</param>
/// <param name="ExpiresAt">到期时间（UTC）。</param>
/// <param name="AdminUserId">租户管理员；为空即未初始化。</param>
/// <param name="EditionId">套餐；为空即不受套餐限制。</param>
/// <param name="Order">排序号。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
/// <param name="UserLimit">租户自己的席位上限，覆盖套餐的；为空即沿用套餐。</param>
/// <param name="StorageLimitMb">租户自己的存储空间上限（MB），覆盖套餐的；为空即沿用套餐。</param>
/// <param name="IsolationMode">隔离方式。</param>
/// <param name="DatabaseProvider">独立库的库类型；为空即与平台库相同。连接串不出现在响应里。</param>
/// <param name="DatabaseStatus">独立库的状态。</param>
/// <param name="DatabaseError">最近一次初始化失败的原因。</param>
public sealed record TenantResponse(
    Guid Id,
    string TenantId,
    string Name,
    string? Description,
    TenantStatus Status,
    DateTimeOffset? ExpiresAt,
    Guid? AdminUserId,
    Guid? EditionId,
    int Order,
    DateTimeOffset CreatedAt,
    int? UserLimit,
    long? StorageLimitMb,
    TenantIsolationMode IsolationMode,
    string? DatabaseProvider,
    TenantDatabaseStatus DatabaseStatus,
    string? DatabaseError)
{
    /// <summary>
    /// 由实体投影。
    /// </summary>
    public static TenantResponse From(SysTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);
        return new TenantResponse(tenant.Id, tenant.TenantId, tenant.Name, tenant.Description, tenant.Status, tenant.ExpiresAt, tenant.AdminUserId, tenant.EditionId, tenant.Order, tenant.CreatedAt, tenant.UserLimit, tenant.StorageLimitMb, tenant.IsolationMode, tenant.DatabaseProvider, tenant.DatabaseStatus, tenant.DatabaseError);
    }
}
