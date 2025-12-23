#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:GenTableColumnDto
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567022
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Dtos.Base;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.CodeGeneration.Dtos.Tables;

/// <summary>
/// 代码生成表列配置 DTO
/// </summary>
public class GenTableColumnDto : CodeGenFullAuditedDtoBase
{
    /// <summary>
    /// 所属表ID
    /// </summary>
    public XiHanBasicAppIdType TableId { get; set; }

    /// <summary>
    /// 数据库列名
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列描述
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 数据库列类型
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// C#类型
    /// </summary>
    public string? CSharpType { get; set; }

    /// <summary>
    /// C#属性名
    /// </summary>
    public string? CSharpProperty { get; set; }

    /// <summary>
    /// TypeScript类型
    /// </summary>
    public string? TsType { get; set; }

    /// <summary>
    /// 列长度
    /// </summary>
    public int? ColumnLength { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否自增
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// 是否列表显示
    /// </summary>
    public bool IsList { get; set; }

    /// <summary>
    /// 是否新增字段
    /// </summary>
    public bool IsInsert { get; set; }

    /// <summary>
    /// 是否编辑字段
    /// </summary>
    public bool IsEdit { get; set; }

    /// <summary>
    /// 是否查询字段
    /// </summary>
    public bool IsQuery { get; set; }

    /// <summary>
    /// 查询方式
    /// </summary>
    public QueryType QueryType { get; set; }

    /// <summary>
    /// 表单显示类型
    /// </summary>
    public HtmlType HtmlType { get; set; }

    /// <summary>
    /// 关联字典类型
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 验证提示信息
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int Sort { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 创建代码生成表列配置 DTO
/// </summary>
public class CreateGenTableColumnDto : CodeGenCreationDtoBase
{
    /// <summary>
    /// 所属表ID
    /// </summary>
    public XiHanBasicAppIdType TableId { get; set; }

    /// <summary>
    /// 数据库列名
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列描述
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 数据库列类型
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// C#类型
    /// </summary>
    public string? CSharpType { get; set; }

    /// <summary>
    /// C#属性名
    /// </summary>
    public string? CSharpProperty { get; set; }

    /// <summary>
    /// TypeScript类型
    /// </summary>
    public string? TsType { get; set; }

    /// <summary>
    /// 列长度
    /// </summary>
    public int? ColumnLength { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; } = false;

    /// <summary>
    /// 是否自增
    /// </summary>
    public bool IsIdentity { get; set; } = false;

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; } = false;

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// 是否列表显示
    /// </summary>
    public bool IsList { get; set; } = true;

    /// <summary>
    /// 是否新增字段
    /// </summary>
    public bool IsInsert { get; set; } = true;

    /// <summary>
    /// 是否编辑字段
    /// </summary>
    public bool IsEdit { get; set; } = true;

    /// <summary>
    /// 是否查询字段
    /// </summary>
    public bool IsQuery { get; set; } = false;

    /// <summary>
    /// 查询方式
    /// </summary>
    public QueryType QueryType { get; set; } = QueryType.Equal;

    /// <summary>
    /// 表单显示类型
    /// </summary>
    public HtmlType HtmlType { get; set; } = HtmlType.Input;

    /// <summary>
    /// 关联字典类型
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

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
/// 更新代码生成表列配置 DTO
/// </summary>
public class UpdateGenTableColumnDto : CodeGenUpdateDtoBase
{
    /// <summary>
    /// 列描述
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// C#类型
    /// </summary>
    public string? CSharpType { get; set; }

    /// <summary>
    /// C#属性名
    /// </summary>
    public string? CSharpProperty { get; set; }

    /// <summary>
    /// TypeScript类型
    /// </summary>
    public string? TsType { get; set; }

    /// <summary>
    /// 是否必填
    /// </summary>
    public bool? IsRequired { get; set; }

    /// <summary>
    /// 是否列表显示
    /// </summary>
    public bool? IsList { get; set; }

    /// <summary>
    /// 是否新增字段
    /// </summary>
    public bool? IsInsert { get; set; }

    /// <summary>
    /// 是否编辑字段
    /// </summary>
    public bool? IsEdit { get; set; }

    /// <summary>
    /// 是否查询字段
    /// </summary>
    public bool? IsQuery { get; set; }

    /// <summary>
    /// 查询方式
    /// </summary>
    public QueryType? QueryType { get; set; }

    /// <summary>
    /// 表单显示类型
    /// </summary>
    public HtmlType? HtmlType { get; set; }

    /// <summary>
    /// 关联字典类型
    /// </summary>
    public string? DictType { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// 最小值
    /// </summary>
    public decimal? MinValue { get; set; }

    /// <summary>
    /// 最大值
    /// </summary>
    public decimal? MaxValue { get; set; }

    /// <summary>
    /// 正则表达式
    /// </summary>
    public string? RegexPattern { get; set; }

    /// <summary>
    /// 验证提示信息
    /// </summary>
    public string? ValidationMessage { get; set; }

    /// <summary>
    /// 排序
    /// </summary>
    public int? Sort { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 数据库列信息 DTO
/// </summary>
public class DbColumnInfoDto
{
    /// <summary>
    /// 列名
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// 列描述
    /// </summary>
    public string? ColumnComment { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public string? DbType { get; set; }

    /// <summary>
    /// 列长度
    /// </summary>
    public int? Length { get; set; }

    /// <summary>
    /// 小数位数
    /// </summary>
    public int? DecimalDigits { get; set; }

    /// <summary>
    /// 是否主键
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// 是否自增
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// 是否可空
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public string? DefaultValue { get; set; }
}
