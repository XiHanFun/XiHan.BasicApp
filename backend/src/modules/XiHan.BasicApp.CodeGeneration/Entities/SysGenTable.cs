#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysGenTable
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567010
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.CodeGeneration.Entities;

/// <summary>
/// 系统代码生成表配置实体
/// </summary>
[SugarTable("Sys_Gen_Table", "系统代码生成表配置表")]
[SugarIndex("IX_SysGenTable_TableName", nameof(TableName), OrderByType.Asc, true)]
[SugarIndex("IX_SysGenTable_ClassName", nameof(ClassName), OrderByType.Asc)]
[SugarIndex("IX_SysGenTable_ModuleName", nameof(ModuleName), OrderByType.Asc)]
public partial class SysGenTable : RbacFullAuditedEntity<long>
{
    /// <summary>
    /// 数据库表名
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库表名", Length = 200, IsNullable = false)]
    public virtual string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 表描述
    /// </summary>
    [SugarColumn(ColumnDescription = "表描述", Length = 500, IsNullable = true)]
    public virtual string? TableComment { get; set; }

    /// <summary>
    /// 实体类名称
    /// </summary>
    [SugarColumn(ColumnDescription = "实体类名称", Length = 200, IsNullable = false)]
    public virtual string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 命名空间
    /// </summary>
    [SugarColumn(ColumnDescription = "命名空间", Length = 500, IsNullable = true)]
    public virtual string? Namespace { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    [SugarColumn(ColumnDescription = "模块名称", Length = 100, IsNullable = true)]
    public virtual string? ModuleName { get; set; }

    /// <summary>
    /// 业务名称
    /// </summary>
    [SugarColumn(ColumnDescription = "业务名称", Length = 100, IsNullable = true)]
    public virtual string? BusinessName { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    [SugarColumn(ColumnDescription = "功能名称", Length = 100, IsNullable = true)]
    public virtual string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    [SugarColumn(ColumnDescription = "作者", Length = 100, IsNullable = true)]
    public virtual string? Author { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    [SugarColumn(ColumnDescription = "模板类型")]
    public virtual TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>
    /// 生成代码方式
    /// </summary>
    [SugarColumn(ColumnDescription = "生成代码方式")]
    public virtual GenType GenType { get; set; } = GenType.Zip;

    /// <summary>
    /// 生成路径
    /// </summary>
    [SugarColumn(ColumnDescription = "生成路径", Length = 500, IsNullable = true)]
    public virtual string? GenPath { get; set; }

    /// <summary>
    /// 父菜单ID
    /// </summary>
    [SugarColumn(ColumnDescription = "父菜单ID", IsNullable = true)]
    public virtual long? ParentMenuId { get; set; }

    /// <summary>
    /// 主键列名
    /// </summary>
    [SugarColumn(ColumnDescription = "主键列名", Length = 100, IsNullable = true)]
    public virtual string? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// 树表父级字段
    /// </summary>
    [SugarColumn(ColumnDescription = "树表父级字段", Length = 100, IsNullable = true)]
    public virtual string? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表名称字段
    /// </summary>
    [SugarColumn(ColumnDescription = "树表名称字段", Length = 100, IsNullable = true)]
    public virtual string? TreeNameColumn { get; set; }

    /// <summary>
    /// 主子表关联主表ID
    /// </summary>
    [SugarColumn(ColumnDescription = "主子表关联主表ID", IsNullable = true)]
    public virtual long? MasterTableId { get; set; }

    /// <summary>
    /// 主子表关联外键列
    /// </summary>
    [SugarColumn(ColumnDescription = "主子表关联外键列", Length = 100, IsNullable = true)]
    public virtual string? MasterForeignKey { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库类型")]
    public virtual DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>
    /// 数据库连接名称
    /// </summary>
    [SugarColumn(ColumnDescription = "数据库连接名称", Length = 100, IsNullable = true)]
    public virtual string? DbConnectionName { get; set; }

    /// <summary>
    /// 生成状态
    /// </summary>
    [SugarColumn(ColumnDescription = "生成状态")]
    public virtual GenStatus GenStatus { get; set; } = GenStatus.NotGenerated;

    /// <summary>
    /// 最后生成时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后生成时间", IsNullable = true)]
    public virtual DateTimeOffset? LastGenTime { get; set; }

    /// <summary>
    /// 扩展选项（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展选项", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Options { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
