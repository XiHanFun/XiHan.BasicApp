// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Authentication.Jwt;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 访问令牌签发命令
/// </summary>
public sealed record AuthAccessTokenIssueCommand(
    SysUser User,
    long? TenantId,
    string SessionBusinessId,
    string AccessTokenJti,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    string? DeviceId);

/// <summary>
/// 访问令牌签发结果
/// </summary>
public sealed record AuthAccessTokenIssueResult(JwtTokenResult TokenResult, LoginTokenDto Token);
