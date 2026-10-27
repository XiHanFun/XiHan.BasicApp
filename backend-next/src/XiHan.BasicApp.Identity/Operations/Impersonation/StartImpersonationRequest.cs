// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// <c>impersonation.start</c> 的请求：以哪个用户、在哪个租户里工作，以及事由（随操作审计落库）。
/// </summary>
/// <param name="TargetUserId">目标用户 ID。</param>
/// <param name="TenantId">目标租户 ID；不填即发起人当前上下文（平台主体是平台，目标须是平台账号；租户主体是自己的租户），填 <c>platform</c> 即平台。租户主体只能填自己所在的租户。</param>
/// <param name="Reason">事由，可空。</param>
public sealed record StartImpersonationRequest(
    Guid TargetUserId,
    [RegularExpression("^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$")] string? TenantId = null,
    [StringLength(256)] string? Reason = null);
