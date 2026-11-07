// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Printing.Domain.Entities;
using XiHan.BasicApp.Printing.Operations.PrintTemplates;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.Printing;

/// <summary>
/// 打印模块登记的字段安全实体
/// </summary>
/// <remarks>
/// 一个实体（打印模板）：列表的查询门控、新建的只读字段与修改、启停的只读字段，
/// 规则由 Access 的字段安全中间件统一套用。字段按详情响应生成（列表项是它去掉模板 JSON 的子集）
/// </remarks>
public static class PrintingFieldSecurity
{
    /// <summary>
    /// 登记本模块的实体与操作
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("printing", PrintingJsonContext.Default, fields => fields
            .Entity<PrintTemplateDetailResponse>("printing.print-templates", "printing.print_templates.title", "printing.print_templates.fields.")
                .Reads(ListPrintTemplates.Id, GetPrintTemplate.Id)
                .Creates(CreatePrintTemplate.Id)
                .Updates<UpdatePrintTemplateRequest>(UpdatePrintTemplate.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysPrintTemplate>(context, request.Id, PrintTemplateDetailResponse.From))
                .Updates<SetPrintTemplateStatusRequest>(SetPrintTemplateStatus.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysPrintTemplate>(context, request.Id, PrintTemplateDetailResponse.From))
                .Keyword("templateCode", "templateName", "dataSourceCode", "remark"));
}
