#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:GenTableDto
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567021
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
/// 代码生成表配置 DTO
/// </summary>
public class GenTableDto : CodeGenFullAuditedDtoBase
{
    /// <summary>
    /// 数据库表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 表描述
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// 实体类名称
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 命名空间
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 业务名称
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    public TemplateType TemplateType { get; set; }

    /// <summary>
    /// 生成代码方式
    /// </summary>
    public GenType GenType { get; set; }

    /// <summary>
    /// 生成路径
    /// </summary>
    public string? GenPath { get; set; }

    /// <summary>
    /// 父菜单ID
    /// </summary>
    public XiHanBasicAppIdType? ParentMenuId { get; set; }

    /// <summary>
    /// 主键列名
    /// </summary>
    public string? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// 树表父级字段
    /// </summary>
    public string? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表名称字段
    /// </summary>
    public string? TreeNameColumn { get; set; }

    /// <summary>
    /// 主子表关联主表ID
    /// </summary>
    public XiHanBasicAppIdType? MasterTableId { get; set; }

    /// <summary>
    /// 主子表关联外键列
    /// </summary>
    public string? MasterForeignKey { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; }

    /// <summary>
    /// 数据库连接名称
    /// </summary>
    public string? DbConnectionName { get; set; }

    /// <summary>
    /// 生成状态
    /// </summary>
    public GenStatus GenStatus { get; set; }

    /// <summary>
    /// 最后生成时间
    /// </summary>
    public DateTimeOffset? LastGenTime { get; set; }

    /// <summary>
    /// 扩展选项
    /// </summary>
    public string? Options { get; set; }

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
/// 代码生成表配置详情 DTO
/// </summary>
public class GenTableDetailDto : GenTableDto
{
    /// <summary>
    /// 列配置列表
    /// </summary>
    public List<GenTableColumnDto> Columns { get; set; } = [];

    /// <summary>
    /// 主表名称（主子表时使用）
    /// </summary>
    public string? MasterTableName { get; set; }

    /// <summary>
    /// 子表列表（主子表时使用）
    /// </summary>
    public List<GenTableDto> SubTables { get; set; } = [];
}

/// <summary>
/// 创建代码生成表配置 DTO
/// </summary>
public class CreateGenTableDto : CodeGenCreationDtoBase
{
    /// <summary>
    /// 数据库表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 表描述
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// 实体类名称
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// 命名空间
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 业务名称
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    public TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>
    /// 生成代码方式
    /// </summary>
    public GenType GenType { get; set; } = GenType.Zip;

    /// <summary>
    /// 生成路径
    /// </summary>
    public string? GenPath { get; set; }

    /// <summary>
    /// 父菜单ID
    /// </summary>
    public XiHanBasicAppIdType? ParentMenuId { get; set; }

    /// <summary>
    /// 数据库类型
    /// </summary>
    public DatabaseType DatabaseType { get; set; } = DatabaseType.MySql;

    /// <summary>
    /// 数据库连接名称
    /// </summary>
    public string? DbConnectionName { get; set; }

    /// <summary>
    /// 扩展选项
    /// </summary>
    public string? Options { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 更新代码生成表配置 DTO
/// </summary>
public class UpdateGenTableDto : CodeGenUpdateDtoBase
{
    /// <summary>
    /// 表描述
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// 实体类名称
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// 命名空间
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// 模块名称
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 业务名称
    /// </summary>
    public string? BusinessName { get; set; }

    /// <summary>
    /// 功能名称
    /// </summary>
    public string? FunctionName { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    public TemplateType? TemplateType { get; set; }

    /// <summary>
    /// 生成代码方式
    /// </summary>
    public GenType? GenType { get; set; }

    /// <summary>
    /// 生成路径
    /// </summary>
    public string? GenPath { get; set; }

    /// <summary>
    /// 父菜单ID
    /// </summary>
    public XiHanBasicAppIdType? ParentMenuId { get; set; }

    /// <summary>
    /// 主键列名
    /// </summary>
    public string? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// 树表父级字段
    /// </summary>
    public string? TreeParentColumn { get; set; }

    /// <summary>
    /// 树表名称字段
    /// </summary>
    public string? TreeNameColumn { get; set; }

    /// <summary>
    /// 主子表关联主表ID
    /// </summary>
    public XiHanBasicAppIdType? MasterTableId { get; set; }

    /// <summary>
    /// 主子表关联外键列
    /// </summary>
    public string? MasterForeignKey { get; set; }

    /// <summary>
    /// 扩展选项
    /// </summary>
    public string? Options { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public YesOrNo? Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }

    /// <summary>
    /// 列配置列表
    /// </summary>
    public List<UpdateGenTableColumnDto>? Columns { get; set; }
}

/// <summary>
/// 导入数据库表 DTO
/// </summary>
public class ImportTableDto
{
    /// <summary>
    /// 数据源ID
    /// </summary>
    public XiHanBasicAppIdType? DataSourceId { get; set; }

    /// <summary>
    /// 表名列表
    /// </summary>
    public List<string> TableNames { get; set; } = [];

    /// <summary>
    /// 默认作者
    /// </summary>
    public string? DefaultAuthor { get; set; }

    /// <summary>
    /// 默认模块名称
    /// </summary>
    public string? DefaultModuleName { get; set; }

    /// <summary>
    /// 默认命名空间
    /// </summary>
    public string? DefaultNamespace { get; set; }
}

/// <summary>
/// 预览代码 DTO
/// </summary>
public class PreviewCodeDto
{
    /// <summary>
    /// 表ID
    /// </summary>
    public XiHanBasicAppIdType TableId { get; set; }

    /// <summary>
    /// 模板ID列表（为空则使用所有启用的模板）
    /// </summary>
    public List<XiHanBasicAppIdType>? TemplateIds { get; set; }
}

/// <summary>
/// 生成代码 DTO
/// </summary>
public class GenerateCodeDto
{
    /// <summary>
    /// 表ID列表
    /// </summary>
    public List<XiHanBasicAppIdType> TableIds { get; set; } = [];

    /// <summary>
    /// 模板ID列表（为空则使用所有启用的模板）
    /// </summary>
    public List<XiHanBasicAppIdType>? TemplateIds { get; set; }

    /// <summary>
    /// 生成方式
    /// </summary>
    public GenType GenType { get; set; } = GenType.Zip;

    /// <summary>
    /// 自定义生成路径
    /// </summary>
    public string? CustomPath { get; set; }
}

/// <summary>
/// 代码预览结果 DTO
/// </summary>
public class CodePreviewResultDto
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 文件名
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件路径
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 代码内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 文件类型
    /// </summary>
    public string FileType { get; set; } = string.Empty;
}

/// <summary>
/// 数据库表信息 DTO
/// </summary>
public class DbTableInfoDto
{
    /// <summary>
    /// 表名
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// 表描述
    /// </summary>
    public string? TableComment { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTimeOffset? CreateTime { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTimeOffset? UpdateTime { get; set; }
}
