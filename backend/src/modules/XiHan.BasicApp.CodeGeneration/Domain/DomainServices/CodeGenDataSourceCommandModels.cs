#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenDataSourceCommandModels
// Guid:c0de9e00-0a02-4a00-9000-000000000a02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 数据源创建命令
/// </summary>
public sealed record CodeGenDataSourceCreateCommand(
    string SourceName,
    string? SourceDescription,
    DatabaseType DatabaseType,
    string Host,
    int Port,
    string DatabaseName,
    string UserName,
    string? Password,
    string? ConnectionString,
    string? ExtraParams,
    int ConnectionTimeout,
    bool IsDefault,
    EnableStatus Status,
    int Sort,
    string? Remark);

/// <summary>
/// 数据源更新命令
/// </summary>
public sealed record CodeGenDataSourceUpdateCommand(
    long BasicId,
    string SourceName,
    string? SourceDescription,
    DatabaseType DatabaseType,
    string Host,
    int Port,
    string DatabaseName,
    string UserName,
    string? Password,
    string? ConnectionString,
    string? ExtraParams,
    int ConnectionTimeout,
    bool IsDefault,
    int Sort,
    string? Remark);

/// <summary>
/// 数据源状态变更命令
/// </summary>
public sealed record CodeGenDataSourceStatusChangeCommand(long BasicId, EnableStatus Status, string? Remark);

/// <summary>
/// 数据源命令结果（包装实体；密码/连接串为加密态，输出前由 Mapper 脱敏）
/// </summary>
public sealed record CodeGenDataSourceCommandResult(SysCodeGenDataSource DataSource);

/// <summary>
/// 数据源连接测试结果
/// </summary>
public sealed record CodeGenDataSourceConnectionTestResult(bool Success, string? Message, long ElapsedMilliseconds);

/// <summary>
/// 数据源运行期连接信息（已解密，供动态连接注册使用）
/// </summary>
/// <param name="ConfigId">连接配置标识（取数据源主键，全局唯一）</param>
/// <param name="DbType">SqlSugar 数据库类型（在此完成映射，避免调用方各自再映一遍）</param>
/// <param name="ConnectionString">连接字符串（明文）</param>
/// <param name="SourceName">数据源名称（用于错误提示）</param>
public sealed record CodeGenDataSourceConnectionInfo(
    string ConfigId,
    DbType DbType,
    string ConnectionString,
    string SourceName);
