// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Domain.Services;

namespace XiHan.BasicApp.Printing.Operations.PrintTemplates;

/// <summary>
/// 打印模板的列表项
/// </summary>
/// <remarks>
/// 不带模板 JSON（列表只要元数据，照 4.x）
/// </remarks>
/// <param name="Id">主键</param>
/// <param name="IsGlobal">是否为平台全局模板</param>
/// <param name="TemplateCode">模板编码</param>
/// <param name="DataSourceCode">数据源编码，为空即自由模板</param>
/// <param name="TemplateName">模板名称</param>
/// <param name="AllowTenantUse">全局模板是否向租户开放</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record PrintTemplateResponse(
    Guid Id,
    bool IsGlobal,
    string TemplateCode,
    string? DataSourceCode,
    string TemplateName,
    bool AllowTenantUse,
    PrintTemplateStatus Status,
    int Order,
    string? Remark,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="template">模板</param>
    public static PrintTemplateResponse From(SysPrintTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return new PrintTemplateResponse(
            template.Id,
            PrintTemplateService.IsPlatform(template.OwnerTenantId),
            template.TemplateCode,
            template.DataSourceCode,
            template.TemplateName,
            template.AllowTenantUse,
            template.Status,
            template.Order,
            template.Remark,
            template.ConcurrencyStamp,
            template.CreatedAt,
            template.ModifiedAt);
    }
}

/// <summary>
/// 打印模板的详情
/// </summary>
/// <param name="Id">主键</param>
/// <param name="IsGlobal">是否为平台全局模板</param>
/// <param name="TemplateCode">模板编码</param>
/// <param name="DataSourceCode">数据源编码，为空即自由模板</param>
/// <param name="TemplateName">模板名称</param>
/// <param name="TemplateJson">打印设计器的模板 JSON</param>
/// <param name="AllowTenantUse">全局模板是否向租户开放</param>
/// <param name="Status">状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
/// <param name="ConcurrencyStamp">并发戳：修改与启停时原样交回</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
/// <param name="ModifiedAt">修改时间（UTC）</param>
public sealed record PrintTemplateDetailResponse(
    Guid Id,
    bool IsGlobal,
    string TemplateCode,
    string? DataSourceCode,
    string TemplateName,
    string TemplateJson,
    bool AllowTenantUse,
    PrintTemplateStatus Status,
    int Order,
    string? Remark,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt)
{
    /// <summary>
    /// 从实体投影
    /// </summary>
    /// <param name="template">模板</param>
    public static PrintTemplateDetailResponse From(SysPrintTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        return new PrintTemplateDetailResponse(
            template.Id,
            PrintTemplateService.IsPlatform(template.OwnerTenantId),
            template.TemplateCode,
            template.DataSourceCode,
            template.TemplateName,
            template.TemplateJson,
            template.AllowTenantUse,
            template.Status,
            template.Order,
            template.Remark,
            template.ConcurrencyStamp,
            template.CreatedAt,
            template.ModifiedAt);
    }
}

/// <summary>
/// 按编码解析出的模板
/// </summary>
/// <param name="Id">模板 ID</param>
/// <param name="TemplateCode">模板编码</param>
/// <param name="DataSourceCode">数据源编码，为空即自由模板</param>
/// <param name="TemplateName">模板名称</param>
/// <param name="TemplateJson">打印设计器的模板 JSON</param>
/// <param name="RequestedScope">请求的作用域</param>
/// <param name="ResolvedScope">实际命中的作用域：<c>Tenant</c> 或 <c>Global</c></param>
/// <param name="ConcurrencyStamp">并发戳：前端据此判断打开样例表单之后模板有没有变</param>
public sealed record ResolvedPrintTemplateResponse(
    Guid Id,
    string TemplateCode,
    string? DataSourceCode,
    string TemplateName,
    string TemplateJson,
    PrintTemplateScope RequestedScope,
    PrintTemplateScope ResolvedScope,
    string ConcurrencyStamp);

/// <summary>
/// 模板编码与数据源编码的文法
/// </summary>
/// <remarks>
/// 不含空白，不超过 100 个字符（照 4.x）
/// </remarks>
public static class PrintTemplateCodes
{
    /// <summary>
    /// 编码的正则
    /// </summary>
    public const string Pattern = "^\\S+$";
}

/// <summary>
/// <c>print-templates.get</c> 的请求
/// </summary>
/// <param name="Id">模板 ID</param>
public sealed record GetPrintTemplateRequest(Guid Id);

/// <summary>
/// <c>print-templates.resolve</c> 的请求
/// </summary>
/// <param name="TemplateCode">模板编码</param>
/// <param name="Scope">作用域，缺省自动</param>
public sealed record ResolvePrintTemplateRequest(
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(PrintTemplateCodes.Pattern)] string TemplateCode,
    PrintTemplateScope Scope = PrintTemplateScope.Auto);

/// <summary>
/// <c>print-templates.create</c> 的请求
/// </summary>
/// <remarks>
/// 写在调用方这一侧（平台即全局模板）；4.x 的写入作用域参数只用来与当前上下文核对，6.0 由一侧决定、不再传
/// </remarks>
/// <param name="TemplateCode">模板编码（本侧唯一、创建后不可改）</param>
/// <param name="TemplateName">模板名称</param>
/// <param name="TemplateJson">打印设计器的模板 JSON</param>
/// <param name="DataSourceCode">数据源编码，为空即自由模板</param>
/// <param name="AllowTenantUse">全局模板是否向租户开放（租户模板忽略）</param>
/// <param name="Status">初始状态</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record CreatePrintTemplateRequest(
    [Required, StringLength(100, MinimumLength = 1), RegularExpression(PrintTemplateCodes.Pattern)] string TemplateCode,
    [Required, StringLength(100, MinimumLength = 1)] string TemplateName,
    [Required] string TemplateJson,
    [StringLength(100), RegularExpression(PrintTemplateCodes.Pattern)] string? DataSourceCode = null,
    bool AllowTenantUse = false,
    PrintTemplateStatus Status = PrintTemplateStatus.Active,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>print-templates.update</c> 的请求
/// </summary>
/// <remarks>
/// 编码不可改；数据源可在自由模板与已登记的数据源之间切换。并发戳与当前的不同即冲突（框架仓储判定）
/// </remarks>
/// <param name="Id">模板 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="TemplateName">模板名称</param>
/// <param name="TemplateJson">打印设计器的模板 JSON</param>
/// <param name="DataSourceCode">数据源编码，为空即自由模板</param>
/// <param name="AllowTenantUse">全局模板是否向租户开放（租户模板忽略）</param>
/// <param name="Order">排序号</param>
/// <param name="Remark">备注</param>
public sealed record UpdatePrintTemplateRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    [Required, StringLength(100, MinimumLength = 1)] string TemplateName,
    [Required] string TemplateJson,
    [StringLength(100), RegularExpression(PrintTemplateCodes.Pattern)] string? DataSourceCode = null,
    bool AllowTenantUse = false,
    [Range(0, int.MaxValue)] int Order = 0,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>print-templates.set-status</c> 的请求
/// </summary>
/// <param name="Id">模板 ID</param>
/// <param name="ConcurrencyStamp">读到的并发戳</param>
/// <param name="Status">目标状态</param>
/// <param name="Remark">操作备注：给了即替换备注，不给保留原备注</param>
public sealed record SetPrintTemplateStatusRequest(
    Guid Id,
    [Required, StringLength(64, MinimumLength = 1)] string ConcurrencyStamp,
    PrintTemplateStatus Status,
    [StringLength(500)] string? Remark = null);

/// <summary>
/// <c>print-templates.delete</c> 的请求：集合入参
/// </summary>
/// <param name="Ids">模板 ID，1 到 100 个</param>
public sealed record PrintTemplateIdsRequest([Required, MinLength(1), MaxLength(100)] IReadOnlyList<Guid> Ids);
