// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 代码生成的外部数据源
/// </summary>
/// <remarks>
/// 外部库的连接定义单独成表、表配置按 <see cref="SysCodeGenTable.DataSourceId"/> 引用（为空即主库）：同一个外部库可以给多张表配置用，导入、同步结构与零代码运行时都回到同一个定义；还被表配置引用的不能删，免得表配置失去来源
/// 平台库实体、只在平台一侧：代码生成是平台的开发工具，不对租户开放
/// 口令与显式连接串经 <c>ISecretProtector</c> 加密存储、响应里不回显（方案 C2），修改时留空即保留原值；但口令留空又改了数据库类型、主机或端口即拒绝，只有改配置权限的人不能把旧口令带到自己指定的新地址上
/// 连接前按出站地址策略核对拼好的连接串里的主机（附加参数也能写主机），防的是把内网地址直接填进来；SQLite 的库名是服务器上的文件路径，要 <c>BasicApp:CodeGeneration:AllowSqliteDataSource</c> 打开才能用
/// 连接在框架的运行时数据源里按 <c>codegen-&lt;ID&gt;</c> 登记、复用并各带一条健康项，修改、启停与删除之后注销，下次使用按新定义重建：不每次请求新建连接，也不会拿着旧定义去连
/// 停用的不能用于导入、同步结构与零代码运行时，也不回落到主库；测试连接停用的也能测，结果写回最近一次测试的三列，列表上就看得出哪个连不上
/// 名称是部分唯一索引，软删除后同名可以重建
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
