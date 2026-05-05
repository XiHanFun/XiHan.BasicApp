#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppCommandDtos
// Guid:2e9a3ba7-25be-4f32-b7db-59032fd31a1c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Application.Dtos;

public sealed class OAuthAppCreateDto
{
    public string AppName { get; set; } = string.Empty;
    public string? AppDescription { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;
    public string GrantTypes { get; set; } = string.Empty;
    public string? RedirectUris { get; set; }
    public string? Scopes { get; set; }
    public int AccessTokenLifetime { get; set; } = 3600;
    public int RefreshTokenLifetime { get; set; } = 2592000;
    public int AuthorizationCodeLifetime { get; set; } = 300;
    public string? Logo { get; set; }
    public string? Homepage { get; set; }
    public bool SkipConsent { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

public sealed class OAuthAppUpdateDto : BasicAppUDto
{
    public string AppName { get; set; } = string.Empty;
    public string? AppDescription { get; set; }
    public OAuthAppType AppType { get; set; } = OAuthAppType.Web;
    public string GrantTypes { get; set; } = string.Empty;
    public string? RedirectUris { get; set; }
    public string? Scopes { get; set; }
    public int AccessTokenLifetime { get; set; } = 3600;
    public int RefreshTokenLifetime { get; set; } = 2592000;
    public int AuthorizationCodeLifetime { get; set; } = 300;
    public string? Logo { get; set; }
    public string? Homepage { get; set; }
    public bool SkipConsent { get; set; }
    public string? Remark { get; set; }
}

public sealed class OAuthAppStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

public sealed class OAuthAppSecretDto
{
    public long BasicId { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
