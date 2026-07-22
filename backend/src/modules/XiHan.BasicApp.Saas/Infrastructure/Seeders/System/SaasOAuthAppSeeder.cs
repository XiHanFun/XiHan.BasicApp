// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Identity;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Seeders;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// SaaS 内建 OAuth 应用种子数据
/// </summary>
/// <remarks>
/// 登录会话签发的 SysOAuthToken.ClientId 引用 <see cref="SaasOAuthClientIds.Web"/>，
/// 本种子保证该客户端在 SysOAuthApp 中有对应注册记录（数据完整性）。
/// 已存在不覆盖：回调地址、令牌有效期等允许运营调整，种子只负责首次落地。
/// </remarks>
public sealed class SaasOAuthAppSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasOAuthAppSeeder> logger,
    IServiceProvider serviceProvider,
    ICurrentTenant currentTenant)
    : DataSeederBase(clientResolver, logger, serviceProvider)
{
    private readonly ICurrentTenant _currentTenant = currentTenant;

    /// <summary>
    /// 种子数据优先级（晚于配置/模板，无依赖）
    /// </summary>
    public override int Order => 28;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]内建OAuth应用种子数据";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        using var platformScope = _currentTenant.Change(null);
        var client = DbClient;

        var existing = await client.Queryable<SysOAuthApp>()
            .FirstAsync(app => app.ClientId == SaasOAuthClientIds.Web);
        if (existing is not null)
        {
            Logger.LogInformation("内建 OAuth 应用 {ClientId} 已存在，跳过种子数据", SaasOAuthClientIds.Web);
            return;
        }

        var webApp = new SysOAuthApp
        {
            TenantId = 0,
            AppName = "BasicApp Web 前端",
            AppDescription = "平台自营 Web 前端（第一方公开客户端），密码登录签发 Token 的默认客户端",
            ClientId = SaasOAuthClientIds.Web,
            // 公开客户端（浏览器侧 SPA）不持有密钥：留空并以授权模式+第一方信任约束
            ClientSecret = string.Empty,
            AppType = OAuthAppType.Web,
            GrantTypes = "password,refresh_token",
            RedirectUris = null,
            Scopes = SaasOAuthClientIds.DefaultScope,
            AccessTokenLifetime = 3600,
            // 与登录会话签发的刷新令牌有效期（7 天）保持一致
            RefreshTokenLifetime = 604800,
            AuthorizationCodeLifetime = 300,
            SkipConsent = true,
            Status = EnableStatus.Enabled,
            Remark = "系统初始化内建 OAuth 应用"
        };

        _ = await client.Insertable(webApp).ExecuteReturnEntityAsync();
        Logger.LogInformation("成功初始化内建 OAuth 应用 {ClientId}", SaasOAuthClientIds.Web);
    }
}
