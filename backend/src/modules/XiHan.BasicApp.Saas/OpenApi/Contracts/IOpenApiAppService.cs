#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOpenApiAppService
// Guid:8d26f04a-1b73-4e95-a0c8-52f9d1e3b7a6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.OpenApi.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.OpenApi.Contracts;

/// <summary>
/// 开放接口应用服务接口（面向第三方签名调用）
/// </summary>
public interface IOpenApiAppService : IApplicationService
{
    /// <summary>
    /// 连通性自测：验签通过即回显调用方身份与服务器时间
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>自测结果</returns>
    Task<OpenApiPingDto> PingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回显自测：原样回显入参，用于验证请求体完整性与内容签名
    /// </summary>
    /// <param name="input">回显入参</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>自测结果</returns>
    Task<OpenApiEchoDto> EchoAsync(OpenApiEchoInputDto input, CancellationToken cancellationToken = default);
}
