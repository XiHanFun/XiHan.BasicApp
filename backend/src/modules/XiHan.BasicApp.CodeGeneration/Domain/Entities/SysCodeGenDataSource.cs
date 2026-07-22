// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 系统代码生成数据源实体
/// 代码生成器的外部数据库连接配置：供扫描目标库元信息并生成对应实体/DTO 代码
/// </summary>
/// <remarks>
/// 关联：
/// - 反向：SysCodeGenTable.DataSourceId（可选；为空时使用主库元数据）
///
/// 写入：
/// - SourceName 全局唯一（UX_SoNa）
/// - ConnectionString 必须加密存储（含密码），服务层读写走加密适配器
/// - DatabaseType 决定元信息扫描 SQL 方言（MySQL/PostgreSQL/SqlServer/SQLite/Oracle 等）
/// - 测试连接必须通过方可保存
///
/// 查询：
/// - 数据源列表：IX_TeId_St
/// - 按数据库类型筛选：IX_DaTy
///
/// 删除：
/// - 仅软删；删除前必须校验：无 SysCodeGenTable 引用
///
/// 状态：
/// - Status: Yes/No（停用后该数据源不可用于生成）
///
/// 场景：
/// - 多库代码生成（主库 + 报表库 + 业务库）
/// - 逆向工程（已有数据库 → 生成代码）
/// </remarks>
[SugarTable(TableName = "Sys_CodeGen_DataSource", TableDescription = "系统代码生成数据源表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_SoNa", nameof(SourceName), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_DaTy", nameof(DatabaseType), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysCodeGenDataSource : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    [SugarColumn(ColumnName = "Source_Name", ColumnDescription = "数据源名称", Length = 100, IsNullable = false)]
    public virtual string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// 数据源描述
    /// </summary>
    [SugarColumn(ColumnName = "Source_Description", ColumnDescription = "数据源描述", Length = 500, IsNullable = true)]
    public virtual string? SourceDescription { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnName = "Database_Type", ColumnDescription = "数据库类型")]
    public virtual DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>
    /// 主机地址
    /// </summary>
    [SugarColumn(ColumnName = "Host", ColumnDescription = "主机地址", Length = 200, IsNullable = false)]
    public virtual string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    [SugarColumn(ColumnName = "Port", ColumnDescription = "端口")]
    public virtual int Port { get; set; } = 3306;

    /// <summary>
    /// 数据库名称
    /// </summary>
    [SugarColumn(ColumnName = "Database_Name", ColumnDescription = "数据库名称", Length = 100, IsNullable = false)]
    public virtual string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnName = "User_Name", ColumnDescription = "用户名", Length = 100, IsNullable = false)]
    public virtual string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码（加密存储）
    /// </summary>
    [SugarColumn(ColumnName = "Password", ColumnDescription = "密码", Length = 500, IsNullable = true)]
    public virtual string? Password { get; set; }

    /// <summary>
    /// 连接字符串
    /// </summary>
    [SugarColumn(ColumnName = "Connection_String", ColumnDescription = "连接字符串", Length = 1000, IsNullable = true)]
    public virtual string? ConnectionString { get; set; }

    /// <summary>
    /// 额外参数
    /// </summary>
    [SugarColumn(ColumnName = "Extra_Params", ColumnDescription = "额外参数", Length = 500, IsNullable = true)]
    public virtual string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    [SugarColumn(ColumnName = "Connection_Timeout", ColumnDescription = "连接超时时间（秒）")]
    public virtual int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 是否默认数据源
    /// </summary>
    [SugarColumn(ColumnName = "Is_Default", ColumnDescription = "是否默认数据源")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 最后测试时间
    /// </summary>
    [SugarColumn(ColumnName = "Last_Test_Time", ColumnDescription = "最后测试时间", IsNullable = true)]
    public virtual DateTimeOffset? LastTestTime { get; set; }

    /// <summary>
    /// 最后测试结果
    /// </summary>
    [SugarColumn(ColumnName = "Last_Test_Result", ColumnDescription = "最后测试结果")]
    public virtual bool LastTestResult { get; set; } = false;

    /// <summary>
    /// 最后测试消息
    /// </summary>
    [SugarColumn(ColumnName = "Last_Test_Message", ColumnDescription = "最后测试消息", Length = 500, IsNullable = true)]
    public virtual string? LastTestMessage { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
