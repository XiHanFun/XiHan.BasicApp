#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TenantEnums
// Guid:1a28152c-d6e9-4396-addb-b479254bad90
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/8/14 9:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Rbac.Enums;

/// <summary>
/// 租户隔离模式
/// </summary>
public enum TenantIsolationMode
{
    /// <summary>
    /// 字段隔离 - 通过TenantId字段区分租户数据
    /// </summary>
    Field = 0,

    /// <summary>
    /// 数据库隔离 - 每个租户使用独立数据库
    /// </summary>
    Database = 1,

    /// <summary>
    /// Schema隔离 - 在同一数据库中使用不同Schema
    /// </summary>
    Schema = 2
}

/// <summary>
/// 租户配置状态
/// </summary>
public enum TenantConfigStatus
{
    /// <summary>
    /// 待配置
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 配置中
    /// </summary>
    Configuring = 1,

    /// <summary>
    /// 已配置
    /// </summary>
    Configured = 2,

    /// <summary>
    /// 配置失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已停用
    /// </summary>
    Disabled = 4
}

/// <summary>
/// 租户数据库类型
/// </summary>
public enum TenantDatabaseType
{
    /// <summary>
    /// SQL Server
    /// </summary>
    SqlServer = 0,

    /// <summary>
    /// MySQL
    /// </summary>
    MySQL = 1,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    PostgreSQL = 2,

    /// <summary>
    /// SQLite
    /// </summary>
    SQLite = 3,

    /// <summary>
    /// Oracle
    /// </summary>
    Oracle = 4
}
