// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Web.Api.Security.OpenApi;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 请求的客户端类型与应用标识
/// </summary>
public static class SaasConditionClient
{
    /// <summary>
    /// 客户端类型：开放接口签名客户端优先，其次令牌里的 client_id（OAuth 应用），再次 session_id（网页登录）
    /// </summary>
    /// <param name="httpContext">请求；认证之后才有令牌声明</param>
    /// <returns>类型（见 <see cref="SaasConditionClientKinds"/>）与应用标识；没有请求时为空</returns>
    public static (string Kind, string? AppId)? Resolve(HttpContext? httpContext)
    {
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items[OpenApiSecurityConstants.SecurityClientContextKey] is OpenApiSecurityClient openApiClient)
        {
            return (SaasConditionClientKinds.OpenApi, openApiClient.AccessKey);
        }

        var user = httpContext.User;
        if (user.FindFirst(XiHanClaimTypes.ClientId)?.Value is { Length: > 0 } clientId)
        {
            return (SaasConditionClientKinds.OAuth, clientId);
        }

        return user.FindFirst(XiHanClaimTypes.SessionId)?.Value is { Length: > 0 }
            ? (SaasConditionClientKinds.Web, null)
            : (SaasConditionClientKinds.Anonymous, null);
    }
}
