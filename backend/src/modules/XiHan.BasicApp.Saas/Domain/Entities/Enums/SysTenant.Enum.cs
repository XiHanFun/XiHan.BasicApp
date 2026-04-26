#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenant.Enum
// Guid:ff0886fb-f997-4bef-ab55-9349f635cd03
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
    [Description("待配置")]
    Pending = 0,

    /// <summary>
    /// 配置中
    /// </summary>
    [Description("配置中")]
    Configuring = 1,

    /// <summary>
    /// 已配置
    /// </summary>
    [Description("已配置")]
    Configured = 2,

    /// <summary>
    /// 配置失败
    /// </summary>
    [Description("配置失败")]
    Failed = 3,

    /// <summary>
    /// 已停用
    /// </summary>
    [Description("已停用")]
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
    [Description("SQL Server")]
    SqlServer = 0,

    /// <summary>
    /// MySql
    /// </summary>
    [Description("MySql")]
    MySql = 1,

    /// <summary>
    /// PostgreSql
    /// </summary>
    [Description("PostgreSql")]
    PostgreSql = 2,

    /// <summary>
    /// SQLite
    /// </summary>
    [Description("SQLite")]
    SQLite = 3,

    /// <summary>
    /// Oracle
    /// </summary>
    [Description("Oracle")]
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
    [Description("字段隔离 - 通过TenantId字段区分租户数据")]
    Field = 0,

    /// <summary>
    /// 数据库隔离 - 每个租户使用独立数据库
    /// </summary>
    [Description("数据库隔离 - 每个租户使用独立数据库")]
    Database = 1,

    /// <summary>
    /// Schema隔离 - 在同一数据库中使用不同Schema
    /// </summary>
    [Description("Schema隔离 - 在同一数据库中使用不同Schema")]
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
    [Description("正常")]
    Normal = 0,

    /// <summary>
    /// 暂停
    /// </summary>
    [Description("暂停")]
    Suspended = 1,

    /// <summary>
    /// 过期
    /// </summary>
    [Description("过期")]
    Expired = 2,

    /// <summary>
    /// 禁用
    /// </summary>
    [Description("禁用")]
    Disabled = 3
}
