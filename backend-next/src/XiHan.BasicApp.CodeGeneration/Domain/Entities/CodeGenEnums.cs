// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 数据源、表配置、列配置与模板的启停状态
/// </summary>
/// <remarks>
/// 4.x 的模板有 <c>IsEnabled</c> 与 <c>Status</c> 两个开关，6.0 合成这一个（方案 C2）
/// </remarks>
public enum CodeGenStatus
{
    /// <summary>
    /// 启用
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用
    /// </summary>
    Disabled = 1,
}

/// <summary>
/// 数据源的数据库类型
/// </summary>
/// <remarks>
/// 照 4.x 的五种；建外部库连接时换成 SqlSugar 的数据库类型名（框架运行时数据源按它选驱动）
/// </remarks>
public enum CodeGenDatabaseType
{
    /// <summary>
    /// MySQL
    /// </summary>
    MySql = 0,

    /// <summary>
    /// SQL Server
    /// </summary>
    SqlServer = 1,

    /// <summary>
    /// PostgreSQL
    /// </summary>
    PostgreSql = 2,

    /// <summary>
    /// Oracle
    /// </summary>
    Oracle = 3,

    /// <summary>
    /// SQLite
    /// </summary>
    Sqlite = 4,
}

/// <summary>
/// 生成模式
/// </summary>
/// <remarks>
/// 表配置取单表、树表、主从之一；通用只给模板用，表示适用于全部模式（照 4.x）
/// </remarks>
public enum CodeGenTemplateType
{
    /// <summary>
    /// 单表
    /// </summary>
#pragma warning disable CA1720 // 照 4.x 的线上取值 Single（前端按名称取值）
    Single = 0,
#pragma warning restore CA1720

    /// <summary>
    /// 树表
    /// </summary>
    Tree = 1,

    /// <summary>
    /// 主从
    /// </summary>
    MasterDetail = 2,

    /// <summary>
    /// 通用（只用于模板）
    /// </summary>
    Universal = 3,
}

/// <summary>
/// 生成结果
/// </summary>
public enum CodeGenGenerationStatus
{
    /// <summary>
    /// 未生成
    /// </summary>
    NotGenerated = 0,

    /// <summary>
    /// 已生成
    /// </summary>
    Generated = 1,

    /// <summary>
    /// 生成失败
    /// </summary>
    Failed = 2,
}

/// <summary>
/// 生成产物的交付方式
/// </summary>
public enum CodeGenDeliveryType
{
    /// <summary>
    /// 产物包（存成临时文件下载）
    /// </summary>
    Zip = 0,

    /// <summary>
    /// 写进项目
    /// </summary>
    /// <remarks>
    /// 只在 Development 且配置打开时可用（方案 C1）
    /// </remarks>
    Project = 1,
}
