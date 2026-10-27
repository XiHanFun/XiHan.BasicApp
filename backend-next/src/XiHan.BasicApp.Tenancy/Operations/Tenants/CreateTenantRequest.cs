// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Tenancy.Operations.Tenants;

/// <summary>
/// <c>tenants.create</c> 的请求：开通一个租户（写业务实体并登记进租户目录）；管理员另由 <c>tenants.initialize-admin</c> 初始化。
/// </summary>
/// <param name="TenantId">租户 ID：字母或数字开头，只含字母、数字、<c>.</c>、<c>_</c>、<c>-</c>，最长 64；创建后不可改。</param>
/// <param name="Name">名称：1 到 128 个字符。</param>
/// <param name="Description">说明，可空。</param>
/// <param name="ExpiresAt">到期时间（UTC），可空。</param>
/// <param name="Order">排序号。</param>
public sealed record CreateTenantRequest(
    [Required, StringLength(64, MinimumLength = 1), RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string TenantId,
    [Required, StringLength(128, MinimumLength = 1)] string Name,
    [StringLength(512)] string? Description = null,
    DateTimeOffset? ExpiresAt = null,
    [Range(0, 100_000)] int Order = 0);
