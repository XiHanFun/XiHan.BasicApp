// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.MultiTenancy;

/// <summary>
/// 按自定义域名找租户
/// </summary>
/// <remarks>
/// 由 Tenancy 实现、Settings 消费：登录前按访问的域名显示租户的品牌、预填租户。只用于展示，不参与身份判定（租户只认令牌声明）
/// 没有实现时视为没有租户绑定域名
/// </remarks>
public interface ITenantDomainResolver
{
    /// <summary>
    /// 绑定了 <paramref name="host"/> 的、正常使用中的租户
    /// </summary>
    /// <remarks>
    /// 主机名不分大小写，端口与末尾的点去掉；停用、到期或没有绑定即没有
    /// </remarks>
    /// <param name="host">访问的主机名，可以带端口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户 ID</returns>
    Task<Maybe<string>> FindTenantAsync(string host, CancellationToken cancellationToken);
}
