// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
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
[ActiveUniqueIndex(nameof(SourceName))]
public sealed class SysCodeGenDataSource : AppAuditedEntity
{
    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(100)]
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    [StringLength(500)]
    public string? SourceDescription { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public CodeGenDatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 主机
    /// </summary>
    [StringLength(200)]
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// 库名
    /// </summary>
    /// <remarks>
    /// SQLite 是库文件的路径
    /// </remarks>
    [StringLength(200)]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 账号
    /// </summary>
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 口令（加密存储）
    /// </summary>
    [StringLength(1000)]
    public string? Password { get; set; }

    /// <summary>
    /// 显式连接串（加密存储）
    /// </summary>
    [StringLength(2000)]
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 拼连接串时附加的参数
    /// </summary>
    /// <remarks>
    /// 形如 <c>SslMode=None;Pooling=false</c>，只在没有显式连接串时追加在拼出的连接串之后
    /// </remarks>
    [StringLength(500)]
    public string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时（秒）
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 是否缺省数据源
    /// </summary>
    /// <remarks>
    /// 最多一个，设为缺省时其余的取消
    /// </remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 最近一次测试连接的时间（UTC）
    /// </summary>
    public DateTimeOffset? LastTestAt { get; set; }

    /// <summary>
    /// 最近一次测试连接是否成功
    /// </summary>
    public bool LastTestResult { get; set; }

    /// <summary>
    /// 最近一次测试连接的结果说明
    /// </summary>
    [StringLength(500)]
    public string? LastTestMessage { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    /// <remarks>
    /// 停用的数据源不能用于导入与零代码运行时
    /// </remarks>
    public CodeGenStatus Status { get; set; } = CodeGenStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
