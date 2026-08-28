// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Jwt;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 访问令牌签发命令
/// </summary>
/// <remarks>
/// <see cref="User"/> 是令牌代表的身份（模仿场景下即被模仿者）；
/// 模仿者四项非空时额外写入 <c>impersonator_*</c> 声明。
/// </remarks>
public sealed record AuthAccessTokenIssueCommand(
    SysUser User,
    long? TenantId,
    string SessionBusinessId,
    string AccessTokenJti,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    string? DeviceId,
    long? ImpersonatorUserId = null,
    string? ImpersonatorUserName = null,
    long? ImpersonatorTenantId = null,
    string? ImpersonatorTenantName = null);

/// <summary>
/// 访问令牌签发结果
/// </summary>
public sealed record AuthAccessTokenIssueResult(JwtTokenResult TokenResult, LoginTokenDto Token);
