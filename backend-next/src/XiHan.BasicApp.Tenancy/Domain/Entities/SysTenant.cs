// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户（平台库业务实体）。
/// </summary>
/// <remarks>
/// 套餐、到期、配额、状态归这里；租户目录（ID、显示名、是否启用、分片连接）是框架的 <c>TenantRecord</c>，开通 / 启停 / 改名 / 配置独立库时在同一工作单元里同步。
/// <see cref="TenantId"/> 是目录键（登录与令牌里的 <c>tenant</c>），创建后不可改。
/// </remarks>
public sealed class SysTenant : AppAuditedEntity
{
    /// <summary>
    /// 租户 ID（目录键）：字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64。
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 说明。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 状态。
    /// </summary>
    public TenantStatus Status { get; set; } = TenantStatus.Active;

    /// <summary>
    /// 到期时间（UTC）。
    /// </summary>
    /// <remarks>
    /// 为空即不限。到期处理随套餐批次加入。
    /// </remarks>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 租户管理员（<c>tenants.initialize-admin</c> 写入）。
    /// </summary>
    /// <remarks>
    /// 为空即未初始化。
    /// </remarks>
    public Guid? AdminUserId { get; set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 租户绑定的套餐（<see cref="SysEdition"/>）。
    /// </summary>
    /// <remarks>
    /// 有效权限与它的白名单求交。为空即不受套餐限制（只能经 <c>tenants.change-edition</c> 显式选择）；开通时不指定即用缺省套餐。
    /// </remarks>
    public Guid? EditionId { get; set; }

    /// <summary>
    /// 租户自己的席位上限，覆盖套餐的。
    /// </summary>
    /// <remarks>
    /// 为空即沿用套餐。
    /// </remarks>
    public int? UserLimit { get; set; }

    /// <summary>
    /// 租户自己的存储空间上限（MB），覆盖套餐的。
    /// </summary>
    /// <remarks>
    /// 为空即沿用套餐。
    /// </remarks>
    public long? StorageLimitMb { get; set; }
    /// <summary>
    /// 隔离方式。
    /// </summary>
    public TenantIsolationMode IsolationMode { get; set; }

    /// <summary>
    /// 独立库的库类型。
    /// </summary>
    /// <remarks>
    /// <c>postgresql</c>、<c>sqlite</c>、<c>mysql</c> 或 <c>sqlserver</c>；为空即与平台库相同。
    /// </remarks>
    [StringLength(32)]
    public string? DatabaseProvider { get; set; }

    /// <summary>
    /// 独立库的连接串（密文）。
    /// </summary>
    /// <remarks>
    /// 经 <c>ISecretProtector</c> 加密存放，同步进租户目录时仍是密文，由 Runtime 的分片解析器解密；不进任何响应。
    /// </remarks>
    [StringLength(2048)]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 独立库的状态。
    /// </summary>
    public TenantDatabaseStatus DatabaseStatus { get; set; }

    /// <summary>
    /// 最近一次初始化失败的原因。
    /// </summary>
    [StringLength(1024)]
    public string? DatabaseError { get; set; }

    /// <summary>
    /// 独立库就绪的时间（UTC）。
    /// </summary>
    public DateTimeOffset? DatabaseInitializedAt { get; set; }
}
