// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户（平台库业务实体）：套餐、到期、配额、状态归这里；租户目录（ID、显示名、是否启用、分片连接）是框架的 <c>TenantRecord</c>，开通 / 启停 / 改名时在同一工作单元里同步。
/// <see cref="TenantId"/> 是目录键（登录与令牌里的 <c>tenant</c>），创建后不可改。
/// </summary>
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
    /// 到期时间（UTC）；为空即不限。到期处理随套餐批次加入。
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>
    /// 租户管理员（<c>tenants.initialize-admin</c> 写入）；为空即未初始化。
    /// </summary>
    public Guid? AdminUserId { get; set; }

    /// <summary>
    /// 排序号。
    /// </summary>
    public int Order { get; set; }
}
