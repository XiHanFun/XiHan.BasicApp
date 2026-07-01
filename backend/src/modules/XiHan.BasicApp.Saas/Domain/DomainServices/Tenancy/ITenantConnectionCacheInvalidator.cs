#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantConnectionCacheInvalidator
// Guid:7c2f9a34-6e18-4b05-9d3a-8f4c1b7e6a25
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户连接缓存失效器
/// </summary>
/// <remarks>
/// 租户隔离模式或连接字符串变更后须调用，使运行时连接提供器丢弃对应租户的缓存描述符，
/// 否则新配置要等应用重启才生效（如刚从字段隔离切到库隔离却仍走平台库）。
/// </remarks>
public interface ITenantConnectionCacheInvalidator
{
    /// <summary>
    /// 使指定租户的连接缓存失效
    /// </summary>
    /// <param name="tenantId">租户标识</param>
    void Invalidate(long tenantId);
}
