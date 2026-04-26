#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenant.Enum
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

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
    /// MySql
    /// </summary>
    MySql = 1,

    /// <summary>
    /// PostgreSql
    /// </summary>
    PostgreSql = 2,

    /// <summary>
    /// SQLite
    /// </summary>
    SQLite = 3,

    /// <summary>
    /// Oracle
    /// </summary>
    Oracle = 4
}

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
/// 租户状态枚举
/// </summary>
public enum TenantStatus
{
    /// <summary>
    /// 正常
    /// </summary>
    Normal = 0,

    /// <summary>
    /// 暂停
    /// </summary>
    Suspended = 1,

    /// <summary>
    /// 过期
    /// </summary>
    Expired = 2,

    /// <summary>
    /// 禁用
    /// </summary>
    Disabled = 3
}

