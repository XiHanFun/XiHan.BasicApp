#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenTable
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567010
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.CodeGeneration.Domain.Entities;

/// <summary>
/// 系统代码生成表配置实体
/// 单张目标表的代码生成配置主体：承载类名映射、模块归属、生成选项；列级配置在 SysCodeGenTableColumn
/// </summary>
/// <remarks>
/// 关联：
/// - DataSourceId → SysCodeGenDataSource（可选）
/// - 反向：SysCodeGenTableColumn.TableId、SysCodeGenHistory.TableId
///
/// 写入：
/// - TableName 全局唯一（UX_TaNa），禁止同一目标表重复配置
/// - ClassName / ModuleName 影响生成后的命名空间与目录层级
/// - 初次导入时自动从数据库元信息填充列；后续可由用户手工调整
///
/// 查询：
/// - 代码生成入口列表：IX_TeId_St
/// - 按模块分组：IX_MoNa
/// - 按类名搜索：IX_ClNa
///
/// 删除：
/// - 仅软删；删除时级联软删 SysCodeGenTableColumn
///
/// 状态：
/// - Status: Yes/No
///
/// 场景：
/// - 数据库表 → 实体/DTO/API/前端页面全栈生成
/// - 重复生成前保留历史配置
/// </remarks>
[SugarTable(TableName = "Sys_CodeGen_Table", TableDescription = "系统代码生成表配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TaNa", nameof(TableName), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_ClNa", nameof(ClassName), OrderByType.Asc)]
[SugarIndex("IX_{table}_MoNa", nameof(ModuleName), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
public partial class SysCodeGenTable : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 数据库表名
    /// </summary>
    [SugarColumn(ColumnName = "Table_Name", ColumnDescription = "数据库表名", Length = 200, IsNullable = false)]
    public virtual string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 表描述
    /// </summary>
    [SugarColumn(ColumnName = "Table_Comment", ColumnDescription = "表描述", Length = 500, IsNullable = true)]
    public virtual string? TableComment { get; set; }

    /// <summary>
    /// 实体类名称
    /// </summary>
    [SugarColumn(ColumnName = "Class_Name", ColumnDescription = "实体类名称", Length = 200, IsNullable = false)]
    public virtual string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 命名空间
    /// </summary>
    [SugarColumn(ColumnName = "Namespace", ColumnDescription = "命名空间", Length = 500, IsNullable = true)]
    public virtual string? Namespace { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    [SugarColumn(ColumnName = "Module_Name", ColumnDescription = "模块名称", Length = 100, IsNullable = true)]
    public virtual string? ModuleName { get; set; }

    /// <summary>
    /// 业务名称
    /// </summary>
    [SugarColumn(ColumnName = "Business_Name", ColumnDescription = "业务名称", Length = 100, IsNullable = true)]
    public virtual string? BusinessName { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    [SugarColumn(ColumnName = "Function_Name", ColumnDescription = "功能名称", Length = 100, IsNullable = true)]
    public virtual string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    [SugarColumn(ColumnName = "Author", ColumnDescription = "作者", Length = 100, IsNullable = true)]
    public virtual string? Author { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    [SugarColumn(ColumnName = "Template_Type", ColumnDescription = "模板类型")]
    public virtual TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>
    /// 生成代码方式
    /// </summary>
    [SugarColumn(ColumnName = "Gen_Type", ColumnDescription = "生成代码方式")]
    public virtual GenType GenType { get; set; } = GenType.Zip;

    /// <summary>
    /// 生成范围（裁剪前端/后端产物）
    /// </summary>
    [SugarColumn(ColumnName = "Generation_Scope", ColumnDescription = "生成范围")]
    public virtual GenerationScope GenerationScope { get; set; } = GenerationScope.All;

    /// <summary>
    /// 包含操作（逗号分隔的操作键，裁剪生成的接口与按钮）
    /// </summary>
    /// <remarks>
    /// 取值为 create/update/delete 的子集（列表/详情为读取基线，始终生成）。
    /// null/空 = 全开（未配置或全选）；非空则按该子集裁剪写接口与按钮。
    /// 仅做功能裁剪——不需要写接口时别生成；不用于改写行为（改行为走 M0 的 override + base.XxxAsync）。
    /// </remarks>
    [SugarColumn(ColumnName = "Enabled_Actions", ColumnDescription = "包含操作", Length = 200, IsNullable = true)]
    public virtual string? EnabledActions { get; set; }

    /// <summary>
    /// 生成路径
    /// </summary>
    [SugarColumn(ColumnName = "Gen_Path", ColumnDescription = "生成路径", Length = 500, IsNullable = true)]
    public virtual string? GenPath { get; set; }

    /// <summary>
    /// 父菜单ID
    /// </summary>
    [SugarColumn(ColumnName = "Parent_Menu_Id", ColumnDescription = "父菜单ID", IsNullable = true)]
    public virtual long? ParentMenuId { get; set; }

    /// <summary>
    /// 主键列名
    /// </summary>
    [SugarColumn(ColumnName = "Primary_Key_Column", ColumnDescription = "主键列名", Length = 100, IsNullable = true)]
    public virtual string? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// 树表父级字段
    /// </summary>
    [SugarColumn(ColumnName = "Tree_Parent_Column", ColumnDescription = "树表父级字段", Length = 100, IsNullable = true)]
    public virtual string? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表名称字段
    /// </summary>
    [SugarColumn(ColumnName = "Tree_Name_Column", ColumnDescription = "树表名称字段", Length = 100, IsNullable = true)]
    public virtual string? TreeNameColumn { get; set; }

    /// <summary>
    /// 主子表关联主表ID
    /// </summary>
    [SugarColumn(ColumnName = "Master_Table_Id", ColumnDescription = "主子表关联主表ID", IsNullable = true)]
    public virtual long? MasterTableId { get; set; }

    /// <summary>
    /// 主子表关联外键列
    /// </summary>
    [SugarColumn(ColumnName = "Master_Foreign_Key", ColumnDescription = "主子表关联外键列", Length = 100, IsNullable = true)]
    public virtual string? MasterForeignKey { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    [SugarColumn(ColumnName = "Database_Type", ColumnDescription = "数据库类型")]
    public virtual DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>
    /// 数据源标识
    /// 指向 SysCodeGenDataSource；为空表示本系统主库。同步表结构与重新生成时据此定位来源库
    /// </summary>
    [SugarColumn(ColumnName = "Data_Source_Id", ColumnDescription = "数据源标识", IsNullable = true)]
    public virtual long? DataSourceId { get; set; }

    /// <summary>
    /// 生成状态
    /// </summary>
    [SugarColumn(ColumnName = "Gen_Status", ColumnDescription = "生成状态")]
    public virtual GenStatus GenStatus { get; set; } = GenStatus.NotGenerated;

    /// <summary>
    /// 最后生成时间
    /// </summary>
    [SugarColumn(ColumnName = "Last_Gen_Time", ColumnDescription = "最后生成时间", IsNullable = true)]
    public virtual DateTimeOffset? LastGenTime { get; set; }

    /// <summary>
    /// 扩展选项（JSON格式）
    /// </summary>
    [SugarColumn(ColumnName = "Options", ColumnDescription = "扩展选项", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? Options { get; set; }

    /// <summary>
    /// 已人工修改的字段名集合（JSON 数组）
    /// 记录被用户手工改过的字段；同步表结构时冻结这些字段，其余跟随最新表结构重新推断
    /// </summary>
    [SugarColumn(ColumnName = "User_Modified_Fields", ColumnDescription = "已人工修改字段", Length = 1000, IsNullable = true)]
    public virtual string? UserModifiedFields { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
