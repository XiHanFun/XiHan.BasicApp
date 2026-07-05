#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:McpApiKeyEndpointFilter
// Guid:a11c0de0-8002-4a10-9a00-00000000ai81
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.Security.Cryptography;
using System.Text;

namespace XiHan.BasicApp.WebHost.Mcp;

/// <summary>
/// MCP 端点 API Key 过滤器（应用管理的 key；在 MCP 处理器/SSE 流开启前校验，不匹配即 401）
/// </summary>
/// <remarks>
/// 端点过滤器在路由与鉴权中间件之后、处理器之前运行一次;配合 <c>AllowAnonymous()</c> 绕过框架全局 FallbackPolicy,
/// 改由本 key 校验守门。定长比较防时序侧信道。接受请求头(默认 X-Api-Key)或 Authorization: Bearer。
/// </remarks>
public sealed class McpApiKeyEndpointFilter : IEndpointFilter
{
    private readonly byte[] _expectedKey;
    private readonly string _headerName;

    /// <summary>
    /// 构造函数
    /// </summary>
    public McpApiKeyEndpointFilter(string apiKey, string headerName)
    {
        _expectedKey = Encoding.UTF8.GetBytes(apiKey);
        _headerName = headerName;
    }

    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.HttpContext.Request;
        var provided = request.Headers[_headerName].ToString();
        if (string.IsNullOrEmpty(provided))
        {
            var authorization = request.Headers.Authorization.ToString();
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                provided = authorization["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrEmpty(provided) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), _expectedKey))
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
