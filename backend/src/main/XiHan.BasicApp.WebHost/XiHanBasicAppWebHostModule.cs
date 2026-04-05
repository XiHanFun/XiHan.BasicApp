#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppWebHostModule
// Guid:c3d5fae5-17b2-44f8-aaa2-1ce1b868f8e6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 05:34:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Saas;
using XiHan.BasicApp.Saas.Hubs;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Authentication.OAuth;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Web.Core.Extensions;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Extensions;

namespace XiHan.BasicApp.WebHost;

/// <summary>
/// 曦寒基础应用 Web 主机
/// </summary>
[DependsOn(
    // 应用模块依赖
    typeof(XiHanBasicAppRbacModule),
    typeof(XiHanBasicAppCodeGenerationModule)
)]
public class XiHanBasicAppWebHostModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var config = services.GetConfiguration();

        // 配置JWT认证
        var jwtOptions = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
        if (jwtOptions != null && !string.IsNullOrEmpty(jwtOptions.SecretKey))
        {
            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwtOptions.ValidateIssuer,
                    ValidateAudience = jwtOptions.ValidateAudience,
                    ValidateLifetime = jwtOptions.ValidateLifetime,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(jwtOptions.ClockSkewMinutes)
                };

                options.Events = new JwtBearerEvents
                {
                    // SignalR WebSocket/SSE 无法携带 Authorization Header，需从 query string 提取 token
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers["Token-Expired"] = "true";
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // 添加 OAuth 第三方登录所需的临时 Cookie scheme
            var oauthOptions = config.GetSection(OAuthOptions.SectionName).Get<OAuthOptions>();
            if (oauthOptions is { Enabled: true })
            {
                authBuilder.AddCookie("ExternalCookie", options =>
                {
                    options.Cookie.Name = ".XiHan.External";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
                });
            }

            // 添加授权：所有未标记 [AllowAnonymous] 的端点默认要求已认证用户
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
        }

        // 配置 CORS（允许前端开发地址跨域）
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(
                        "http://localhost:5888",
                        "http://127.0.0.1:5888")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // 添加控制器
        services.AddControllers();
    }

    /// <summary>
    /// 应用初始化
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        // 映射 SignalR Hub 端点
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapXiHanHub<BasicAppNotificationHub>(SignalRConstants.HubPaths.Notification);
            endpoints.MapXiHanHub<BasicAppChatHub>(SignalRConstants.HubPaths.Chat);
        });
    }
}
