// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户独立库的状态。
/// </summary>
/// <remarks>
/// 对应 4.x 的配置状态：配好连接即待初始化，<c>tenants.initialize-database</c> 成功即就绪、失败记原因可再试。
/// </remarks>
public enum TenantDatabaseStatus
{
    /// <summary>
    /// 共库，不需要独立库。
    /// </summary>
    NotRequired = 0,

    /// <summary>
    /// 待初始化。
    /// </summary>
    Pending = 1,

    /// <summary>
    /// 已就绪。
    /// </summary>
    Ready = 2,

    /// <summary>
    /// 初始化失败。
    /// </summary>
    Failed = 3,
}
