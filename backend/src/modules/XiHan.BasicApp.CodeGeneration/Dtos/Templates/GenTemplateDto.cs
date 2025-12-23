#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:GenTemplateDto
// Guid:a1b2c3d4-e5f6-7890-abcd-ef1234567023
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/23 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Dtos.Base;
using XiHan.BasicApp.CodeGeneration.Enums;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.CodeGeneration.Dtos.Templates;

/// <summary>
/// 代码生成模板 DTO
/// </summary>
public class GenTemplateDto : CodeGenFullAuditedDtoBase
{
    /// <summary>
    /// 模板编码
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? TemplateDescription { get; set; }

    /// <summary>
    /// 模板分组
    /// </summary>
    public string? TemplateGroup { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    public TemplateType TemplateType { get; set; }

    /// <summary>
    /// 模板引擎
    /// </summary>
    public TemplateEngine TemplateEngine { get; set; }

    /// <summary>
    /// 模板内容
    /// </summary>
    public string? TemplateContent { get; set; }

    /// <summary>
    /// 模板路径
    /// </summary>
    public string? TemplatePath { get; set; }

    /// <summary>
    /// 生成文件名表达式
    /// </summary>
    public string? FileNameExpression { get; set; }

    /// <summary>
    /// 生成文件路径表达式
    /// </summary>
    public string? FilePathExpression { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// 是否内置
    /// </summary>
    public bool IsBuiltIn { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; }

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
/// 创建代码生成模板 DTO
/// </summary>
public class CreateGenTemplateDto : CodeGenCreationDtoBase
{
    /// <summary>
    /// 模板编码
    /// </summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>
    /// 模板名称
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? TemplateDescription { get; set; }

    /// <summary>
    /// 模板分组
    /// </summary>
    public string? TemplateGroup { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    public TemplateType TemplateType { get; set; } = TemplateType.Single;

    /// <summary>
    /// 模板引擎
    /// </summary>
    public TemplateEngine TemplateEngine { get; set; } = TemplateEngine.Razor;

    /// <summary>
    /// 模板内容
    /// </summary>
    public string? TemplateContent { get; set; }

    /// <summary>
    /// 模板路径
    /// </summary>
    public string? TemplatePath { get; set; }

    /// <summary>
    /// 生成文件名表达式
    /// </summary>
    public string? FileNameExpression { get; set; }

    /// <summary>
    /// 生成文件路径表达式
    /// </summary>
    public string? FilePathExpression { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool IsEnabled { get; set; } = true;

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
/// 更新代码生成模板 DTO
/// </summary>
public class UpdateGenTemplateDto : CodeGenUpdateDtoBase
{
    /// <summary>
    /// 模板名称
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// 模板描述
    /// </summary>
    public string? TemplateDescription { get; set; }

    /// <summary>
    /// 模板分组
    /// </summary>
    public string? TemplateGroup { get; set; }

    /// <summary>
    /// 模板类型
    /// </summary>
    public TemplateType? TemplateType { get; set; }

    /// <summary>
    /// 模板引擎
    /// </summary>
    public TemplateEngine? TemplateEngine { get; set; }

    /// <summary>
    /// 模板内容
    /// </summary>
    public string? TemplateContent { get; set; }

    /// <summary>
    /// 模板路径
    /// </summary>
    public string? TemplatePath { get; set; }

    /// <summary>
    /// 生成文件名表达式
    /// </summary>
    public string? FileNameExpression { get; set; }

    /// <summary>
    /// 生成文件路径表达式
    /// </summary>
    public string? FilePathExpression { get; set; }

    /// <summary>
    /// 文件扩展名
    /// </summary>
    public string? FileExtension { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool? IsEnabled { get; set; }

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
