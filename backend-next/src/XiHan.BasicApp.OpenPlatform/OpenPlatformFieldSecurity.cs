// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.OpenPlatform.Domain.Entities;
using XiHan.BasicApp.OpenPlatform.Operations.OauthApps;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.OpenPlatform;

/// <summary>
/// 开放平台包登记的字段安全实体。
/// </summary>
/// <remarks>
/// 4.x 同：OAuth 应用（管理面，本人的「我的应用」不套用）。字段由响应类型生成，规则由 Access 的字段安全中间件在读、建、改处统一套用。
/// </remarks>
public static class OpenPlatformFieldSecurity
{
    /// <summary>
    /// 登记本包的实体与操作。
    /// </summary>
    /// <param name="services">服务集合。</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("openplatform", OpenPlatformJsonContext.Default, fields => fields
            .Entity<OAuthAppResponse>("openplatform.oauth-apps", "openplatform.oauth_apps.title", "openplatform.oauth_apps.fields.")
                .Reads(ListOAuthApps.Id, GetOAuthApp.Id)
                .Creates(CreateOAuthApp.Id)
                .Updates<UpdateOAuthAppRequest>(UpdateOAuthApp.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysOAuthApp>(context, request.Id, OAuthAppResponse.From))
                .Keyword("appName", "clientId", "appDescription", "homepage", "remark"));
}
