// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的外部数据源
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧。导入表结构与零代码运行时经它连外部库，表配置的数据源为空即主库
/// 口令与连接串经 <c>ISecretProtector</c> 加密存储、响应里不回显（方案 C2）；
/// 显式连接串优先，没有就按主机、端口、库名、账号拼出。名称唯一，软删除后可以重建
/// </remarks>
[SugarTable(TableName = "sys_code_gen_data_source", TableDescription = "代码生成的外部数据源")]
[ActiveUniqueIndex(nameof(SourceName), Name = "ux_sys_code_gen_data_source_source_name")]
public sealed class SysCodeGenDataSource : AppAuditedEntity
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnName = "source_name", Length = 100, IsNullable = false, ColumnDescription = "名称")]
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [SugarColumn(ColumnName = "source_description", Length = 500, IsNullable = true, ColumnDescription = "描述")]
    public string? SourceDescription { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnName = "database_type", IsNullable = false, ColumnDescription = "数据库类型")]
    public CodeGenDatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 主机
    /// </summary>
    [SugarColumn(ColumnName = "host", Length = 200, IsNullable = false, ColumnDescription = "主机")]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    [SugarColumn(ColumnName = "port", IsNullable = false, ColumnDescription = "端口")]
    public int Port { get; set; }

    /// <summary>
    /// 库名
    /// </summary>
    /// <remarks>
    /// SQLite 是库文件的路径
    /// </remarks>
    [SugarColumn(ColumnName = "database_name", Length = 200, IsNullable = false, ColumnDescription = "库名")]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 账号
    /// </summary>
    [SugarColumn(ColumnName = "user_name", Length = 100, IsNullable = false, ColumnDescription = "账号")]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 口令（加密存储）
    /// </summary>
    [SugarColumn(ColumnName = "password", Length = 1000, IsNullable = true, ColumnDescription = "口令（加密存储）")]
    public string? Password { get; set; }

    /// <summary>
    /// 显式连接串（加密存储）
    /// </summary>
    [SugarColumn(ColumnName = "connection_string", Length = 2000, IsNullable = true, ColumnDescription = "显式连接串（加密存储）")]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 拼连接串时附加的参数
    /// </summary>
    /// <remarks>
    /// 形如 <c>SslMode=None;Pooling=false</c>，只在没有显式连接串时追加在拼出的连接串之后
    /// </remarks>
    [SugarColumn(ColumnName = "extra_params", Length = 500, IsNullable = true, ColumnDescription = "拼连接串时附加的参数")]
    public string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时（秒）
    /// </summary>
    [SugarColumn(ColumnName = "connection_timeout", IsNullable = false, ColumnDescription = "连接超时（秒）")]
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 是否缺省数据源
    /// </summary>
    /// <remarks>
    /// 最多一个，设为缺省时其余的取消
    /// </remarks>
    [SugarColumn(ColumnName = "is_default", IsNullable = false, ColumnDescription = "是否缺省数据源")]
    public bool IsDefault { get; set; }

    /// <summary>
    /// 最近一次测试连接的时间（UTC）
    /// </summary>
    [SugarColumn(ColumnName = "last_test_at", IsNullable = true, ColumnDescription = "最近一次测试连接的时间（UTC）")]
    public DateTimeOffset? LastTestAt { get; set; }

    /// <summary>
    /// 最近一次测试连接是否成功
    /// </summary>
    [SugarColumn(ColumnName = "last_test_result", IsNullable = false, ColumnDescription = "最近一次测试连接是否成功")]
    public bool LastTestResult { get; set; }

    /// <summary>
    /// 最近一次测试连接的结果说明
    /// </summary>
    [SugarColumn(ColumnName = "last_test_message", Length = 500, IsNullable = true, ColumnDescription = "最近一次测试连接的结果说明")]
    public string? LastTestMessage { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    /// <remarks>
    /// 停用的数据源不能用于导入与零代码运行时
    /// </remarks>
    [SugarColumn(ColumnName = "status", IsNullable = false, ColumnDescription = "状态")]
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    [SugarColumn(ColumnName = "order", IsNullable = false, ColumnDescription = "排序号")]
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "remark", Length = 500, IsNullable = true, ColumnDescription = "备注")]
    public string? Remark { get; set; }
}
