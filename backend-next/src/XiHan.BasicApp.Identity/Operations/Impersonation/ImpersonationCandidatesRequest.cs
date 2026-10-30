// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Paging;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// <c>impersonation.candidates</c> 的请求。
/// </summary>
/// <remarks>
/// 在哪个租户里找、按什么关键字找、取哪一页（查询串，平铺的页码与页大小）。
/// </remarks>
/// <param name="TenantId">目标租户 ID；不填即发起人当前上下文（平台主体是平台、租户主体是自己的租户），填 <c>platform</c> 即平台。租户主体只能填自己所在的租户。</param>
/// <param name="Keyword">关键字，匹配登录名与显示名；可空。</param>
/// <param name="PageIndex">页码，从 1 起。</param>
/// <param name="PageSize">页大小，1 到 100。</param>
public sealed record ImpersonationCandidatesRequest(
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string? TenantId = null,
    [StringLength(64)] string? Keyword = null,
    [Range(1, int.MaxValue)] int PageIndex = 1,
    [Range(1, 100)] int PageSize = PageInfo.DefaultSize);
