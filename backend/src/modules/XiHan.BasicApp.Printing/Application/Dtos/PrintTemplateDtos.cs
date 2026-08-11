// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Printing.Application.Dtos;

/// <summary>
/// 打印模板创建 DTO；租户归属由当前上下文决定，不允许调用方指定任意租户。
/// </summary>
public sealed class PrintTemplateCreateDto : BasicAppCDto
{
    /// <summary>写入作用域。</summary>
    [EnumDataType(typeof(PrintTemplateScope))]
    public PrintTemplateScope Scope { get; set; } = PrintTemplateScope.Auto;

    /// <summary>不可变模板编码。</summary>
    [Required]
    [StringLength(100)]
    [RegularExpression("^\\S+$")]
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>可选代码数据源编码；为空表示自由模板。</summary>
    [StringLength(100)]
    [RegularExpression("^\\S+$")]
    public string? DataSourceCode { get; set; }

    /// <summary>模板名称。</summary>
    [Required]
    [StringLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>hiprint 模板 JSON。</summary>
    [Required]
    public string TemplateJson { get; set; } = string.Empty;

    /// <summary>hiprint 引擎版本。</summary>
    [Required]
    [StringLength(32)]
    public string EngineVersion { get; set; } = "0.0.60";

    /// <summary>全局模板是否允许租户使用。</summary>
    public bool AllowTenantUse { get; set; }

    /// <summary>初始状态。</summary>
    [EnumDataType(typeof(EnableStatus))]
    public EnableStatus Status { get; set; } = EnableStatus.Enabled;

    /// <summary>显示排序。</summary>
    [Range(0, int.MaxValue)]
    public int Sort { get; set; }

    /// <summary>备注。</summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 打印模板更新 DTO；模板编码不可变，数据源可选且可调整。
/// </summary>
public sealed class PrintTemplateUpdateDto : BasicAppUDto
{
    /// <summary>写入作用域。</summary>
    [EnumDataType(typeof(PrintTemplateScope))]
    public PrintTemplateScope Scope { get; set; } = PrintTemplateScope.Auto;

    /// <summary>客户端读取到的十进制字符串行版本。</summary>
    [Required]
    [RegularExpression("^[0-9]+$")]
    public string RowVersion { get; set; } = string.Empty;

    /// <summary>可选代码数据源编码；为空表示自由模板。</summary>
    [StringLength(100)]
    [RegularExpression("^\\S+$")]
    public string? DataSourceCode { get; set; }

    /// <summary>模板名称。</summary>
    [Required]
    [StringLength(100)]
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>hiprint 模板 JSON。</summary>
    [Required]
    public string TemplateJson { get; set; } = string.Empty;

    /// <summary>hiprint 引擎版本。</summary>
    [Required]
    [StringLength(32)]
    public string EngineVersion { get; set; } = "0.0.60";

    /// <summary>全局模板是否允许租户使用。</summary>
    public bool AllowTenantUse { get; set; }

    /// <summary>显示排序。</summary>
    [Range(0, int.MaxValue)]
    public int Sort { get; set; }

    /// <summary>备注。</summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 打印模板启停 DTO。
/// </summary>
public sealed class PrintTemplateStatusUpdateDto : BasicAppUDto
{
    /// <summary>写入作用域。</summary>
    [EnumDataType(typeof(PrintTemplateScope))]
    public PrintTemplateScope Scope { get; set; } = PrintTemplateScope.Auto;

    /// <summary>客户端读取到的十进制字符串行版本。</summary>
    [Required]
    [RegularExpression("^[0-9]+$")]
    public string RowVersion { get; set; } = string.Empty;

    /// <summary>目标状态。</summary>
    [EnumDataType(typeof(EnableStatus))]
    public EnableStatus Status { get; set; }

    /// <summary>可选操作备注。</summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}

/// <summary>
/// 打印模板删除 DTO，要求调用方提交最后读取到的行版本。
/// </summary>
public sealed class PrintTemplateDeleteDto : BasicAppUDto
{
    /// <summary>写入作用域。</summary>
    [EnumDataType(typeof(PrintTemplateScope))]
    public PrintTemplateScope Scope { get; set; } = PrintTemplateScope.Auto;

    /// <summary>客户端读取到的十进制字符串行版本。</summary>
    [Required]
    [RegularExpression("^[0-9]+$")]
    public string RowVersion { get; set; } = string.Empty;
}

/// <summary>
/// 打印模板列表项 DTO。
/// </summary>
public class PrintTemplateListItemDto : BasicAppDto
{
    /// <summary>模板编码。</summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>可选代码数据源编码；为空表示自由模板。</summary>
    public string? DataSourceCode { get; set; }

    /// <summary>模板名称。</summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>hiprint 引擎版本。</summary>
    public string EngineVersion { get; set; } = string.Empty;

    /// <summary>是否为平台全局模板。</summary>
    public bool IsGlobal { get; set; }

    /// <summary>全局模板是否允许租户使用。</summary>
    public bool AllowTenantUse { get; set; }

    /// <summary>启停状态。</summary>
    public EnableStatus Status { get; set; }

    /// <summary>显示排序。</summary>
    public int Sort { get; set; }

    /// <summary>备注。</summary>
    public string? Remark { get; set; }

    /// <summary>十进制字符串行版本，避免 JavaScript 整数精度问题。</summary>
    public string RowVersion { get; set; } = string.Empty;
}

/// <summary>
/// 打印模板详情 DTO。
/// </summary>
public sealed class PrintTemplateDetailDto : PrintTemplateListItemDto
{
    /// <summary>hiprint 模板 JSON。</summary>
    public string TemplateJson { get; set; } = string.Empty;

    /// <summary>创建时间。</summary>
    public DateTimeOffset CreatedTime { get; set; }

    /// <summary>最后修改时间。</summary>
    public DateTimeOffset? ModifiedTime { get; set; }
}

/// <summary>
/// 按编码解析得到的可打印模板 DTO。
/// </summary>
public sealed class ResolvedPrintTemplateDto
{
    /// <summary>模板主键。</summary>
    public long BasicId { get; set; }

    /// <summary>模板编码。</summary>
    public string TemplateCode { get; set; } = string.Empty;

    /// <summary>可选数据源编码；为空表示自由模板。</summary>
    public string? DataSourceCode { get; set; }

    /// <summary>模板名称。</summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>hiprint 模板 JSON。</summary>
    public string TemplateJson { get; set; } = string.Empty;

    /// <summary>hiprint 引擎版本。</summary>
    public string EngineVersion { get; set; } = string.Empty;

    /// <summary>请求的解析作用域。</summary>
    public PrintTemplateScope RequestedScope { get; set; }

    /// <summary>最终命中的明确作用域。</summary>
    public PrintTemplateScope ResolvedScope { get; set; }

    /// <summary>十进制字符串行版本。</summary>
    public string RowVersion { get; set; } = string.Empty;
}

/// <summary>
/// 打印模板分页查询 DTO。
/// </summary>
public sealed class PrintTemplatePageQueryDto : BasicAppPRDto
{
    /// <summary>查询作用域。</summary>
    [EnumDataType(typeof(PrintTemplateScope))]
    public PrintTemplateScope Scope { get; set; } = PrintTemplateScope.Auto;

    /// <summary>编码、名称、数据源或备注关键字。</summary>
    [StringLength(200)]
    public string? Keyword { get; set; }

    /// <summary>可选启停状态。</summary>
    [EnumDataType(typeof(EnableStatus))]
    public EnableStatus? Status { get; set; }
}
