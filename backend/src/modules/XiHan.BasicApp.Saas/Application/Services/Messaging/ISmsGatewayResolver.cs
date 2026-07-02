#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsGatewayResolver
// Guid:9b4d7f20-6e83-4a15-8c9d-0f3e5a7b2c61
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 15:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 短信网关解析器
/// </summary>
/// <remarks>
/// 镜像存储 Provider 范式：每次解析读取「默认且启用」的 <c>SysSmsConfig</c>，
/// 按配置指纹缓存已构建的网关客户端——改配置即重建（热生效），无需缓存失效事件。
/// 无可用配置时返回 <c>null</c>，调用方须 fail-closed（拒绝发送并记 Failed），杜绝静默假成功。
/// </remarks>
public interface ISmsGatewayResolver
{
    /// <summary>
    /// 解析当前租户的默认短信网关客户端
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>网关客户端；无「默认且启用」配置时返回 null</returns>
    Task<ISmsGatewayClient?> ResolveAsync(CancellationToken cancellationToken = default);
}
