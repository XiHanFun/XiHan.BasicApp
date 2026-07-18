#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OpenApiAppService
// Guid:c07b5e13-9a48-42df-8b60-1e4a7c95f2d8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Saas.Application;
using XiHan.BasicApp.Saas.OpenApi.Contracts;
using XiHan.BasicApp.Saas.OpenApi.Dtos;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Web.Api.Middlewares;
using XiHan.Framework.Web.Api.Security.OpenApi;

namespace XiHan.BasicApp.Saas.OpenApi.AppServices;

/// <summary>
/// 开放接口应用服务（面向第三方签名调用的自测端点）
/// </summary>
/// <remarks>
/// 与其它应用服务一样按约定自动注册为动态 API：实现 <see cref="IOpenApiAppService"/>（<c>IApplicationService</c>）即被扫描暴露，
/// 无需在模块里手工映射端点。类级 <c>RouteTemplate = "api/openapi"</c> 把路由钉在开放接口专属前缀下，
/// 与 <c>XiHan:Web:Api:OpenApiSecurity:ProtectedPathPrefixes</c> 对齐，从而受签名中间件强制保护。
/// <para>
/// <c>[AllowAnonymous]</c> 覆盖基类的 <c>[Authorize]</c>：开放接口的身份由 <b>HMAC 签名</b>确立（无 JWT），
/// 验签通过后 <see cref="XiHanOpenApiSecurityMiddleware"/> 会把调用方放进 <c>HttpContext.Items</c>，此处回显以证明链路通畅。
/// </para>
/// </remarks>
[AllowAnonymous]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "开放接口", RouteTemplate = "api/openapi")]
public sealed class OpenApiAppService
    : SaasApplicationService, IOpenApiAppService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OpenApiAppService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    [HttpGet]
    public Task<OpenApiPingDto> PingAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var client = ResolveSecurityClient();
        return Task.FromResult(new OpenApiPingDto
        {
            Ok = true,
            Message = "OpenAPI 签名校验通过",
            AccessKey = client?.AccessKey,
            OwnerUserId = client?.OwnerUserId,
            ServerTimeUtc = DateTimeOffset.UtcNow
        });
    }

    /// <inheritdoc />
    [HttpPost]
    public Task<OpenApiEchoDto> EchoAsync(OpenApiEchoInputDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var client = ResolveSecurityClient();
        return Task.FromResult(new OpenApiEchoDto
        {
            Ok = true,
            Message = "OpenAPI 签名校验通过",
            AccessKey = client?.AccessKey,
            Echo = input,
            ServerTimeUtc = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// 取签名中间件验签通过后放入上下文的调用方信息
    /// </summary>
    private OpenApiSecurityClient? ResolveSecurityClient()
    {
        return _httpContextAccessor.HttpContext?.Items[OpenApiSecurityConstants.SecurityClientContextKey] as OpenApiSecurityClient;
    }
}
