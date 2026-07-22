// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 租户数据库初始化器（库隔离显式建库动作）
/// </summary>
public interface ITenantDatabaseInitializer
{
    /// <summary>
    /// 为库隔离租户初始化独立数据库：建库 → 建表 → 基线种子（幂等）。
    /// </summary>
    /// <param name="tenantId">租户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>初始化后的租户（含最新配置状态）</returns>
    Task<SysTenant> InitializeAsync(long tenantId, CancellationToken cancellationToken = default);
}
