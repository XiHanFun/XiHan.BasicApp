#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ITenantDatabaseInitializer
// Guid:2f8a4c6b-1d09-4e73-9b52-7c3a0f6d8e41
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 11:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
