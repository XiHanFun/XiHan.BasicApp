// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Tenancy.Domain.Entities;

/// <summary>
/// 租户数据的隔离方式。
/// </summary>
/// <remarks>
/// 方案 5.10：Field（共库、按租户列过滤，缺省）与 Database（落租户库的实体在租户自己的库里）；4.x 的 Schema 隔离未实装，6.0 不保留。
/// </remarks>
public enum TenantIsolationMode
{
    /// <summary>
    /// 共库，按租户列隔离。
    /// </summary>
    Field = 0,

    /// <summary>
    /// 独立库。
    /// </summary>
    Database = 1,
}
