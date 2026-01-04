#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:GenDataSourceDto
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567024
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Dtos.Base;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.CodeGeneration.Dtos.DataSources;

/// <summary>
/// 代码生成数据源 DTO
/// </summary>
public class GenDataSourceDto : CodeGenFullAuditedDtoBase
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// 数据源描述
    /// </summary>
    public string? SourceDescription { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 主机地址
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 额外参数
    /// </summary>
    public string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; }

    /// <summary>
    /// 是否默认数据源
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 最后测试时间
    /// </summary>
    public DateTimeOffset? LastTestTime { get; set; }

    /// <summary>
    /// 最后测试结果
    /// </summary>
    public bool LastTestResult { get; set; }

    /// <summary>
    /// 最后测试消息
    /// </summary>
    public string? LastTestMessage { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建代码生成数据源 DTO
/// </summary>
public class CreateGenDataSourceDto : CodeGenCreationDtoBase
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// 数据源描述
    /// </summary>
    public string? SourceDescription { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>
    /// 主机地址
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 连接字符串（可选，优先使用）
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 额外参数
    /// </summary>
    public string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 是否默认数据源
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新代码生成数据源 DTO
/// </summary>
public class UpdateGenDataSourceDto : CodeGenUpdateDtoBase
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// 数据源描述
    /// </summary>
    public string? SourceDescription { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType? DatabaseType { get; set; }

    /// <summary>
    /// 主机地址
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// 端口
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 额外参数
    /// </summary>
    public string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int? ConnectionTimeout { get; set; }

    /// <summary>
    /// 是否默认数据源
    /// </summary>
    public bool? IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 测试数据源连接 DTO
/// </summary>
public class TestDataSourceDto
{
    /// <summary>
    /// 数据源ID（如果已存在）
    /// </summary>
    public long? DataSourceId { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>
    /// 主机地址
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; } = 3306;

    /// <summary>
    /// 数据库名称
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// 连接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 额外参数
    /// </summary>
    public string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;
}

/// <summary>
/// 测试连接结果 DTO
/// </summary>
public class TestConnectionResultDto
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 数据库版本
    /// </summary>
    public string? DatabaseVersion { get; set; }

    /// <summary>
    /// 连接耗时（毫秒）
    /// </summary>
    public long Duration { get; set; }
}
