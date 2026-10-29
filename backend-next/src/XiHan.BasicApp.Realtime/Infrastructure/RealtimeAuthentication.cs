// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.Framework.Security.Jwt;

namespace XiHan.BasicApp.Realtime.Infrastructure;

/// <summary>
/// Hub 的 ASP.NET Core 认证。
/// </summary>
/// <remarks>
/// 方案 5.11：内核管道的认证管不到绕过管道的 Hub，这里用 JWT Bearer 方案按 <see cref="JwtOptions"/> 校验同一份访问令牌，
/// 校验项与框架的 <see cref="JwtAuthenticationHandler"/> 一致。方案不设为缺省：只有带 <see cref="Policy"/> 的端点（Hub）走它，操作与其他普通端点照旧由内核认证。
/// </remarks>
internal static class RealtimeAuthentication
{
    /// <summary>
    /// 认证方案名。
    /// </summary>
    public const string Scheme = "Realtime";

    /// <summary>
    /// Hub 端点的授权策略：经 <see cref="Scheme"/> 认证且已认证。
    /// </summary>
    public const string Policy = "basicapp.realtime";

    public static void AddRealtimeAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication().AddJwtBearer(Scheme, static _ => { });
        services.AddOptions<JwtBearerOptions>(Scheme).Configure<JwtOptions>(static (options, jwt) =>
        {
            // 声明按 JWT 原始类型名进入主体（sub、sid、tenant、role），与内核认证一致
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateLifetime = true,
                // JwtBearer 缺省容差 5 分钟，这里取框架配置的值
                ClockSkew = jwt.ClockSkew,
                ValidateIssuer = jwt.Issuer is not null,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = jwt.Audience is not null,
                ValidAudience = jwt.Audience,
                NameClaimType = jwt.NameClaim,
                RoleClaimType = "role",
            };
            options.Events = new JwtBearerEvents
            {
                // WebSocket 与 SSE 带不了请求头，SignalR 客户端把令牌放在查询串 access_token 里；只在 Hub 路径下接受
                OnMessageReceived = static context =>
                {
                    var token = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments(RealtimeHubs.Prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        context.Token = token;
                    }

                    return Task.CompletedTask;
                },
            };
        });
        services.AddAuthorizationBuilder().AddPolicy(Policy, static policy => policy.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser());
    }
}
