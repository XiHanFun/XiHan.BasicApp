#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysCodeGenTableColumn
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567011
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
/// 系统代码生成表列配置实体
/// 单列的生成配置：承载 C# 类型映射、字段别名、必填/长度、UI 表单属性（组件类型/查询方式）
/// </summary>
/// <remarks>
/// 关联：
/// - TableId → SysCodeGenTable（多对一）
///
/// 写入：
/// - TableId + ColumnName 建议组合唯一（虽未显式 UX，服务层校验）
/// - 列信息由表导入时自动填充；用户可补充：FormType/QueryType/字段显示名/脱敏策略
/// - 类型映射表（DB 类型 → C# 类型）由生成器统一维护
///
/// 查询：
/// - 表列清单：IX_TaId + ORDER BY Sort
/// - 按列名搜索：IX_CoNa
///
/// 删除：
/// - 仅软删；随 SysCodeGenTable 级联处理
///
/// 状态：
/// - Status: Yes/No（仅影响代码生成时是否包含此列）
///
/// 场景：
/// - 列级别精细控制（是否生成、显示名、表单组件类型）
/// - 脱敏字段标记（生成的 DTO 自动应用 MaskStrategy）
/// </remarks>
[SugarTable(TableName = "Sys_CodeGen_TableColumn", TableDescription = "系统代码生成表列配置表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TaId", nameof(TableId), OrderByType.Asc)]
[SugarIndex("IX_{table}_CoNa", nameof(ColumnName), OrderByType.Asc)]
public partial class SysCodeGenTableColumn : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 所属表ID
    /// </summary>
    [SugarColumn(ColumnName = "Table_Id", ColumnDescription = "所属表ID", IsNullable = false)]
    public virtual long TableId { get; set; }

    /// <summary>
    /// 数据库列名
    /// </summary>
    [SugarColumn(ColumnName = "Column_Name", ColumnDescription = "数据库列名", Length = 200, IsNullable = false)]
    public virtual string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列描述
    /// </summary>
    [SugarColumn(ColumnName = "Column_Comment", ColumnDescription = "列描述", Length = 500, IsNullable = true)]
    public virtual string? ColumnComment { get; set; }

    /// <summary>
    /// 数据库列类型
    /// </summary>
    [SugarColumn(ColumnName = "Column_Type", ColumnDescription = "数据库列类型", Length = 100, IsNullable = true)]
    public virtual string? ColumnType { get; set; }

    /// <summary>
    /// C#类型
    /// </summary>
    [SugarColumn(ColumnName = "CSharp_Type", ColumnDescription = "C#类型", Length = 100, IsNullable = true)]
    public virtual string? CSharpType { get; set; }

    /// <summary>
    /// C#属性名
    /// </summary>
    [SugarColumn(ColumnName = "CSharp_Property", ColumnDescription = "C#属性名", Length = 200, IsNullable = true)]
    public virtual string? CSharpProperty { get; set; }

    /// <summary>
    /// TypeScript类型
    /// </summary>
    [SugarColumn(ColumnName = "Ts_Type", ColumnDescription = "TypeScript类型", Length = 100, IsNullable = true)]
    public virtual string? TsType { get; set; }

    /// <summary>
    /// 列长度
    /// </summary>
    [SugarColumn(ColumnName = "Column_Length", ColumnDescription = "列长度", IsNullable = true)]
    public virtual int? ColumnLength { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    [SugarColumn(ColumnName = "Decimal_Digits", ColumnDescription = "小数位数", IsNullable = true)]
    public virtual int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    [SugarColumn(ColumnName = "Is_Primary_Key", ColumnDescription = "是否主键")]
    public virtual bool IsPrimaryKey { get; set; } = false;

    /// <summary>
    /// 是否自增
    /// </summary>
    [SugarColumn(ColumnName = "Is_Identity", ColumnDescription = "是否自增")]
    public virtual bool IsIdentity { get; set; } = false;

    /// <summary>
    /// 是否可空
    /// </summary>
    [SugarColumn(ColumnName = "Is_Nullable", ColumnDescription = "是否可空")]
    public virtual bool IsNullable { get; set; } = false;

    /// <summary>
    /// 是否必填
    /// </summary>
    [SugarColumn(ColumnName = "Is_Required", ColumnDescription = "是否必填")]
    public virtual bool IsRequired { get; set; } = false;

    /// <summary>
    /// 是否列表显示
    /// </summary>
    [SugarColumn(ColumnName = "Is_List", ColumnDescription = "是否列表显示")]
    public virtual bool IsList { get; set; } = true;

    /// <summary>
    /// 是否新增字段
    /// </summary>
    [SugarColumn(ColumnName = "Is_Insert", ColumnDescription = "是否新增字段")]
    public virtual bool IsInsert { get; set; } = true;

    /// <summary>
    /// 是否编辑字段
    /// </summary>
    [SugarColumn(ColumnName = "Is_Edit", ColumnDescription = "是否编辑字段")]
    public virtual bool IsEdit { get; set; } = true;

    /// <summary>
    /// 是否查询字段
    /// </summary>
    [SugarColumn(ColumnName = "Is_Query", ColumnDescription = "是否查询字段")]
    public virtual bool IsQuery { get; set; } = false;

    /// <summary>
    /// 查询方式
    /// </summary>
    [SugarColumn(ColumnName = "Query_Type", ColumnDescription = "查询方式")]
    public virtual QueryType QueryType { get; set; } = QueryType.Equal;

    /// <summary>
    /// 表单显示类型
    /// </summary>
    [SugarColumn(ColumnName = "Html_Type", ColumnDescription = "表单显示类型")]
    public virtual HtmlType HtmlType { get; set; } = HtmlType.Input;

    /// <summary>
    /// 字典选择器类型（字典/枚举/常量三分；决定 DictCode/EnumTypeName/ConstValues 哪个生效，空表示非选项列）
    /// </summary>
    [SugarColumn(ColumnName = "Dict_Selector_Type", ColumnDescription = "字典选择器类型", IsNullable = true)]
    public virtual DictSelectorType? DictSelectorType { get; set; }

    /// <summary>
    /// 字典码（DictSelector 时生效，关联系统字典类型编码）
    /// </summary>
    [SugarColumn(ColumnName = "Dict_Code", ColumnDescription = "字典码", Length = 200, IsNullable = true)]
    public virtual string? DictCode { get; set; }

    /// <summary>
    /// 枚举类型全名（EnumSelector 时生效，如 XiHan.BasicApp.Saas.Domain.Enums.EnableStatus）
    /// </summary>
    [SugarColumn(ColumnName = "Enum_Type_Name", ColumnDescription = "枚举类型全名", Length = 500, IsNullable = true)]
    public virtual string? EnumTypeName { get; set; }

    /// <summary>
    /// 常量项 JSON（ConstSelector 时生效，如 [{"value":1,"label":"启用"}]）
    /// </summary>
    [SugarColumn(ColumnName = "Const_Values", ColumnDescription = "常量项JSON", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ConstValues { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    [SugarColumn(ColumnName = "Default_Value", ColumnDescription = "默认值", Length = 500, IsNullable = true)]
    public virtual string? DefaultValue { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    [SugarColumn(ColumnName = "Min_Value", ColumnDescription = "最小值", IsNullable = true)]
    public virtual decimal? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    [SugarColumn(ColumnName = "Max_Value", ColumnDescription = "最大值", IsNullable = true)]
    public virtual decimal? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    [SugarColumn(ColumnName = "Regex_Pattern", ColumnDescription = "正则表达式", Length = 500, IsNullable = true)]
    public virtual string? RegexPattern { get; set; }

    /// <summary>
    /// 验证提示信息
    /// </summary>
    [SugarColumn(ColumnName = "Validation_Message", ColumnDescription = "验证提示信息", Length = 500, IsNullable = true)]
    public virtual string? ValidationMessage { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    [SugarColumn(ColumnName = "Sort", ColumnDescription = "排序")]
    public virtual int Sort { get; set; } = 0;

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnName = "Status", ColumnDescription = "状态")]
    public virtual EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>
    /// 已人工修改的字段名集合（JSON 数组）
    /// 记录本列被用户手工改过的字段；同步表结构时冻结这些字段，其余跟随最新表结构重新推断
    /// </summary>
    [SugarColumn(ColumnName = "User_Modified_Fields", ColumnDescription = "已人工修改字段", Length = 500, IsNullable = true)]
    public virtual string? UserModifiedFields { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
