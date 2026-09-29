// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Identity;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders;

/// <summary>
/// SaaS 内建 OAuth 应用：平台 Web 前端这个第一方客户端
/// </summary>
/// <remarks>
/// 登录签发的令牌引用 <see cref="SaasOAuthClientIds.Web"/>，这里保证它有注册记录。
/// 回调地址、令牌有效期等归运营：只在首次创建时写入，之后不再覆盖。
/// </remarks>
public sealed class SaasOAuthAppSeeder(
    ISqlSugarClientResolver clientResolver,
    ILogger<SaasOAuthAppSeeder> logger,
    IServiceProvider serviceProvider)
    : PlatformDataSeederBase(clientResolver, logger, serviceProvider)
{
    /// <summary>
    /// 种子数据优先级
    /// </summary>
    public override int Order => SeedOrders.PlatformData + 3;

    /// <summary>
    /// 种子数据名称
    /// </summary>
    public override string Name => "[SaaS]内建 OAuth 应用";

    /// <summary>
    /// 种子数据实现
    /// </summary>
    protected override async Task SeedInternalAsync()
    {
        var client = DbClient;
        if (await client.Queryable<SysOAuthApp>().IncludingDeleted().AnyAsync(app => app.ClientId == SaasOAuthClientIds.Web))
        {
            Logger.LogInformation("{Seeder}：{ClientId} 已存在，不覆盖运营的修改", Name, SaasOAuthClientIds.Web);
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
        Logger.LogInformation("{Seeder}：新增 {ClientId}", Name, SaasOAuthClientIds.Web);
    }
}
