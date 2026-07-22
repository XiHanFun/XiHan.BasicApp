// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// OAuth 应用创建命令
/// </summary>
public sealed record OAuthAppCreateCommand(
    string AppName,
    string? AppDescription,
    string ClientId,
    string? ClientSecret,
    OAuthAppType AppType,
    string GrantTypes,
    string? RedirectUris,
    string? Scopes,
    int AccessTokenLifetime,
    int RefreshTokenLifetime,
    int AuthorizationCodeLifetime,
    string? Logo,
    string? Homepage,
    bool SkipConsent,
    EnableStatus Status,
    string? Remark,
    bool IsPublic = false);

/// <summary>
/// OAuth 应用更新命令
/// </summary>
public sealed record OAuthAppUpdateCommand(
    long BasicId,
    string AppName,
    string? AppDescription,
    OAuthAppType AppType,
    string GrantTypes,
    string? RedirectUris,
    string? Scopes,
    int AccessTokenLifetime,
    int RefreshTokenLifetime,
    int AuthorizationCodeLifetime,
    string? Logo,
    string? Homepage,
    bool SkipConsent,
    string? Remark);

/// <summary>
/// OAuth 应用状态变更命令
/// </summary>
public sealed record OAuthAppStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// OAuth 应用命令结果
/// </summary>
public sealed record OAuthAppCommandResult(SysOAuthApp App, string? PlaintextSecret = null);
