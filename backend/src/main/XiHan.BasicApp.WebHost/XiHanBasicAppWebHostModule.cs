#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppWebHostModule
// Guid:c9bf348b-8c2f-4e2a-9f36-cc2edafe551e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2024/12/10 05:34:12
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using XiHan.BasicApp.CodeGeneration;
using XiHan.BasicApp.Rbac;
using XiHan.BasicApp.Web.Core;
using XiHan.Framework.Authentication.Jwt;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Web.Core.Extensions;

namespace XiHan.BasicApp.WebHost;

/// <summary>
/// 曦寒基础应用 Web 主机
/// </summary>
[DependsOn(
    typeof(XiHanBasicAppRbacModule),
    typeof(XiHanBasicAppCodeGenerationModule),
    typeof(XiHanBasicAppWebCoreModule)
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
            services.AddAuthentication(options =>
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

            // 添加授权
            services.AddAuthorization();
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
    }
}
