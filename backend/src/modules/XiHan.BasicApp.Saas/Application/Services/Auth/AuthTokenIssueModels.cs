#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:AuthTokenIssueModels
// Guid:0b7d3d93-82ac-4be2-b997-76043a3cce42
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
