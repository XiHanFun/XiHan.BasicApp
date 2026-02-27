#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenDataSource
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567014
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.CodeGeneration.Entities;

/// <summary>
/// 系统代码生成数据源实体
/// </summary>
[SugarTable("Sys_Code_Gen_Data_Source", "系统代码生成数据源表")]
[SugarIndex("IX_SysCodeGenDataSource_SoNa", nameof(SourceName), OrderByType.Asc, true)]
[SugarIndex("IX_SysCodeGenDataSource_DaTy", nameof(DatabaseType), OrderByType.Asc)]
[SugarIndex("IX_SysCodeGenDataSource_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysCodeGenDataSource : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 数据源名称
    /// </summary>
    [SugarColumn(ColumnDescription = "数据源名称", Length = 100, IsNullable = false)]
    public virtual string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// 数据源描述
    /// </summary>
    [SugarColumn(ColumnDescription = "数据源描述", Length = 500, IsNullable = true)]
    public virtual string? SourceDescription { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库类型")]
    public virtual DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>
    /// 主机地址
    /// </summary>
    [SugarColumn(ColumnDescription = "主机地址", Length = 200, IsNullable = false)]
    public virtual string Host { get; set; } = string.Empty;

    /// <summary>
    /// 端口
    /// </summary>
    [SugarColumn(ColumnDescription = "端口")]
    public virtual int Port { get; set; } = 3306;

    /// <summary>
    /// 数据库名称
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库名称", Length = 100, IsNullable = false)]
    public virtual string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户名", Length = 100, IsNullable = false)]
    public virtual string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密码（加密存储）
    /// </summary>
    [SugarColumn(ColumnDescription = "密码", Length = 500, IsNullable = true)]
    public virtual string? Password { get; set; }

    /// <summary>
    /// 连接字符串
    /// </summary>
    [SugarColumn(ColumnDescription = "连接字符串", Length = 1000, IsNullable = true)]
    public virtual string? ConnectionString { get; set; }

    /// <summary>
    /// 额外参数
    /// </summary>
    [SugarColumn(ColumnDescription = "额外参数", Length = 500, IsNullable = true)]
    public virtual string? ExtraParams { get; set; }

    /// <summary>
    /// 连接超时时间（秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "连接超时时间（秒）")]
    public virtual int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// 是否默认数据源
    /// </summary>
    [SugarColumn(ColumnDescription = "是否默认数据源")]
    public virtual bool IsDefault { get; set; } = false;

    /// <summary>
    /// 最后测试时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后测试时间", IsNullable = true)]
    public virtual DateTimeOffset? LastTestTime { get; set; }

    /// <summary>
    /// 最后测试结果
    /// </summary>
    [SugarColumn(ColumnDescription = "最后测试结果")]
    public virtual bool LastTestResult { get; set; } = false;

    /// <summary>
    /// 最后测试消息
    /// </summary>
    [SugarColumn(ColumnDescription = "最后测试消息", Length = 500, IsNullable = true)]
    public virtual string? LastTestMessage { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
