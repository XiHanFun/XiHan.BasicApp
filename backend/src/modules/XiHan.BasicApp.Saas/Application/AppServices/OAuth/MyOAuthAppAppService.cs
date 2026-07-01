#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:MyOAuthAppAppService
// Guid:9a2e7c63-5d4a-4f3b-b128-4c0e6f9d3a72
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Identity;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 我的 OAuth 应用应用服务（个人中心开发者设置）
/// </summary>
/// <remarks>
/// 自助管理仅限本人创建的应用（按 <c>CreatedId</c> 归属校验），无需管理端权限码；复用管理端
/// <see cref="IOAuthAppDomainService"/> 做校验/落库，仅暴露精简字段，其余（授权类型/范围/时效）用默认值。
/// 与「开放平台/应用管理」管理页并存：管理页面向管理员管全租户，本服务面向普通用户管自己的。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "个人中心-OAuth 应用", RouteTemplate = "api/MyOAuthApp")]
public sealed class MyOAuthAppAppService
    : SaasApplicationService, IMyOAuthAppAppService
{
    /// <summary>自助创建默认授权类型：授权码 + 刷新令牌（公开客户端强制 PKCE）</summary>
    private const string DefaultGrantTypes = "authorization_code,refresh_token";

    private const int DefaultAccessTokenLifetime = 3600;
    private const int DefaultRefreshTokenLifetime = 2592000;
    private const int DefaultAuthorizationCodeLifetime = 300;

    private const string ClientTypePublic = "Public";
    private const string ClientTypeConfidential = "Confidential";

    private readonly IOAuthAppDomainService _oauthAppDomainService;

    private readonly IOAuthAppRepository _oauthAppRepository;

    private readonly ICurrentUser _currentUser;

    /// <summary>
    /// 构造函数
    /// </summary>
    public MyOAuthAppAppService(
        IOAuthAppDomainService oauthAppDomainService,
        IOAuthAppRepository oauthAppRepository,
        ICurrentUser currentUser)
    {
        _oauthAppDomainService = oauthAppDomainService;
        _oauthAppRepository = oauthAppRepository;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 我的 OAuth 应用列表（仅本人创建）
    /// </summary>
    public async Task<List<MyOAuthAppItemDto>> GetMyOAuthAppsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = CurrentUserId();
        var apps = await _oauthAppRepository.GetListAsync(app => app.CreatedId == userId, cancellationToken);
        return [.. apps.OrderByDescending(app => app.CreatedTime).Select(ToItemDto)];
    }

    /// <summary>
    /// 创建我的 OAuth 应用（机密：生成密钥仅返回一次；公开：无密钥，依赖 PKCE）
    /// </summary>
    [UnitOfWork(true)]
    public async Task<MyOAuthAppSecretDto> CreateMyOAuthAppAsync(MyOAuthAppCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(input.RedirectUris))
        {
            throw new UserFriendlyException("授权回调地址不能为空。");
        }

        var isPublic = IsPublicType(input.ClientType);
        var clientId = await GenerateUniqueClientIdAsync(cancellationToken);

        var command = new OAuthAppCreateCommand(
            AppName: input.AppName,
            AppDescription: input.AppDescription,
            ClientId: clientId,
            ClientSecret: null,
            AppType: OAuthAppType.Web,
            GrantTypes: DefaultGrantTypes,
            RedirectUris: input.RedirectUris,
            Scopes: SaasOAuthClientIds.DefaultScope,
            AccessTokenLifetime: DefaultAccessTokenLifetime,
            RefreshTokenLifetime: DefaultRefreshTokenLifetime,
            AuthorizationCodeLifetime: DefaultAuthorizationCodeLifetime,
            Logo: input.Logo,
            Homepage: input.Homepage,
            SkipConsent: false,
            Status: EnableStatus.Enabled,
            Remark: null,
            IsPublic: isPublic);

        OAuthAppCommandResult result;
        try
        {
            result = await _oauthAppDomainService.CreateOAuthAppAsync(command, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }
        catch (ArgumentException ex)
        {
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }

        return new MyOAuthAppSecretDto
        {
            BasicId = result.App.BasicId,
            ClientId = result.App.ClientId,
            ClientType = isPublic ? ClientTypePublic : ClientTypeConfidential,
            ClientSecret = result.PlaintextSecret ?? string.Empty
        };
    }

    /// <summary>
    /// 更新我的 OAuth 应用（仅名称/主页/描述/回调/Logo；授权类型等保持不变）
    /// </summary>
    [UnitOfWork(true)]
    public async Task<MyOAuthAppItemDto> UpdateMyOAuthAppAsync(MyOAuthAppUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(input.RedirectUris))
        {
            throw new UserFriendlyException("授权回调地址不能为空。");
        }

        var app = await GetOwnedOrThrowAsync(input.BasicId, cancellationToken);

        var command = new OAuthAppUpdateCommand(
            BasicId: app.BasicId,
            AppName: input.AppName,
            AppDescription: input.AppDescription,
            AppType: app.AppType,
            GrantTypes: app.GrantTypes,
            RedirectUris: input.RedirectUris,
            Scopes: app.Scopes,
            AccessTokenLifetime: app.AccessTokenLifetime,
            RefreshTokenLifetime: app.RefreshTokenLifetime,
            AuthorizationCodeLifetime: app.AuthorizationCodeLifetime,
            Logo: input.Logo,
            Homepage: input.Homepage,
            SkipConsent: app.SkipConsent,
            Remark: app.Remark);

        OAuthAppCommandResult result;
        try
        {
            result = await _oauthAppDomainService.UpdateOAuthAppAsync(command, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }
        catch (ArgumentException ex)
        {
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }

        return ToItemDto(result.App);
    }

    /// <summary>
    /// 重置我的 OAuth 应用密钥（仅机密客户端；明文仅返回一次）
    /// </summary>
    [UnitOfWork(true)]
    public async Task<MyOAuthAppSecretDto> RegenerateMyOAuthAppSecretAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var app = await GetOwnedOrThrowAsync(id, cancellationToken);
        if (IsPublicApp(app))
        {
            throw new UserFriendlyException("公开客户端不使用密钥，无法重置。");
        }

        var result = await _oauthAppDomainService.RegenerateOAuthAppSecretAsync(id, cancellationToken);
        return new MyOAuthAppSecretDto
        {
            BasicId = result.App.BasicId,
            ClientId = result.App.ClientId,
            ClientType = ClientTypeConfidential,
            ClientSecret = result.PlaintextSecret ?? string.Empty
        };
    }

    /// <summary>
    /// 启用/停用我的 OAuth 应用
    /// </summary>
    [UnitOfWork(true)]
    public async Task<MyOAuthAppItemDto> UpdateMyOAuthAppStatusAsync(MyOAuthAppStatusDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var app = await GetOwnedOrThrowAsync(input.BasicId, cancellationToken);
        var result = await _oauthAppDomainService.UpdateOAuthAppStatusAsync(
            new OAuthAppStatusChangeCommand(app.BasicId, input.Status, app.Remark),
            cancellationToken);
        return ToItemDto(result.App);
    }

    /// <summary>
    /// 删除我的 OAuth 应用（存在授权码/令牌记录时不可删，请改为停用）
    /// </summary>
    [UnitOfWork(true)]
    public async Task DeleteMyOAuthAppAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _ = await GetOwnedOrThrowAsync(id, cancellationToken);
        try
        {
            await _oauthAppDomainService.DeleteOAuthAppAsync(id, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(ex.Message, innerException: ex);
        }
    }

    private static bool IsPublicType(string? clientType)
    {
        return string.Equals(clientType?.Trim(), ClientTypePublic, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicApp(SysOAuthApp app)
    {
        return string.IsNullOrWhiteSpace(app.ClientSecret);
    }

    private static MyOAuthAppItemDto ToItemDto(SysOAuthApp app)
    {
        return new MyOAuthAppItemDto
        {
            BasicId = app.BasicId,
            ClientId = app.ClientId,
            AppName = app.AppName,
            AppDescription = app.AppDescription,
            Homepage = app.Homepage,
            Logo = app.Logo,
            RedirectUris = app.RedirectUris,
            ClientType = IsPublicApp(app) ? ClientTypePublic : ClientTypeConfidential,
            GrantTypes = app.GrantTypes,
            Scopes = app.Scopes,
            Status = app.Status,
            CreatedTime = app.CreatedTime
        };
    }

    private long CurrentUserId()
    {
        return _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
    }

    /// <summary>
    /// 加载本人拥有的应用，否则抛出（不存在 / 非本人创建均拒绝，防越权）
    /// </summary>
    private async Task<SysOAuthApp> GetOwnedOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new UserFriendlyException("OAuth 应用主键无效。");
        }

        var app = await _oauthAppRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new UserFriendlyException("OAuth 应用不存在。");

        var userId = CurrentUserId();
        if (app.CreatedId != userId)
        {
            throw new UserFriendlyException("无权操作该 OAuth 应用。");
        }

        return app;
    }

    /// <summary>
    /// 生成全局唯一的客户端ID（128 位随机十六进制；碰撞概率可忽略，仍做少量重试）
    /// </summary>
    private async Task<string> GenerateUniqueClientIdAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            if (!await _oauthAppRepository.AnyAsync(app => app.ClientId == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        return Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
    }
}
