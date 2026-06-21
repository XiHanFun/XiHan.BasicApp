#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenDataSourceDtos
// Guid:c0de9e00-0401-4a00-9000-000000000401
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

#pragma warning disable CS1591

using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Application.Dtos;

/// <summary>
/// 代码生成数据源创建 DTO
/// </summary>
public sealed class CodeGenDataSourceCreateDto
{
    public string SourceName { get; set; } = string.Empty;
    public string? SourceDescription { get; set; }
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 3306;
    public string DatabaseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? ConnectionString { get; set; }
    public string? ExtraParams { get; set; }
    public int ConnectionTimeout { get; set; } = 30;
    public bool IsDefault { get; set; }
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成数据源更新 DTO
/// </summary>
public sealed class CodeGenDataSourceUpdateDto : BasicAppUDto
{
    public string SourceName { get; set; } = string.Empty;
    public string? SourceDescription { get; set; }
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 3306;
    public string DatabaseName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string? ConnectionString { get; set; }
    public string? ExtraParams { get; set; }
    public int ConnectionTimeout { get; set; } = 30;
    public bool IsDefault { get; set; }
    public int Sort { get; set; }
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成数据源状态更新 DTO
/// </summary>
public sealed class CodeGenDataSourceStatusUpdateDto : BasicAppDto
{
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;
    public string? Remark { get; set; }
}

/// <summary>
/// 代码生成数据源分页查询 DTO
/// </summary>
public sealed class CodeGenDataSourcePageQueryDto : BasicAppPRDto
{
    public string? Keyword { get; set; }
    public DatabaseType? DatabaseType { get; set; }
    public EnableStatus? Status { get; set; }
}

/// <summary>
/// 代码生成数据源列表项 DTO
/// </summary>
public class CodeGenDataSourceListItemDto : BasicAppDto
{
    public string SourceName { get; set; } = string.Empty;
    public string? SourceDescription { get; set; }
    public DatabaseType DatabaseType { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTimeOffset? LastTestTime { get; set; }
    public bool LastTestResult { get; set; }
    public string? LastTestMessage { get; set; }
    public EnableStatus Status { get; set; }
    public int Sort { get; set; }
    public string? Remark { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
}

/// <summary>
/// 代码生成数据源详情 DTO（含连接明细，密码字段输出时须脱敏）
/// </summary>
public sealed class CodeGenDataSourceDetailDto : CodeGenDataSourceListItemDto
{
    public string UserName { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public string? ExtraParams { get; set; }
    public int ConnectionTimeout { get; set; }
    public long? CreatedId { get; set; }
    public string? CreatedBy { get; set; }
    public long? ModifiedId { get; set; }
    public string? ModifiedBy { get; set; }
}

/// <summary>
/// 连接测试结果 DTO
/// </summary>
public sealed class CodeGenConnectionTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
