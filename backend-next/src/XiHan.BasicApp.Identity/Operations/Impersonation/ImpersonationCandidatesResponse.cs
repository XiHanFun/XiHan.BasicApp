// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Identity.Operations.Impersonation;

/// <summary>
/// <c>impersonation.candidates</c> 的响应：最多 20 个候选，按登录名排序。
/// </summary>
/// <param name="Items">候选用户。</param>
public sealed record ImpersonationCandidatesResponse(IReadOnlyList<ImpersonationCandidateResponse> Items);
