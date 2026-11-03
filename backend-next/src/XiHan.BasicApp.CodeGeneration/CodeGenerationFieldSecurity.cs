// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenDatasources;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTables;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenTemplates;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Runtime.Queries;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成模块的字段安全登记
/// </summary>
/// <remarks>
/// 照 4.x：数据源（新建、修改、启停）、表配置（修改、启停）与模板（新建、修改、启停）登记字段安全，规则由 Access 的中间件与响应过滤器统一套用
/// </remarks>
public static class CodeGenerationFieldSecurity
{
    /// <summary>
    /// 登记本模块的实体与操作
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("codegeneration", CodeGenerationJsonContext.Default, fields => fields
            .Entity<CodegenDatasourceDetailResponse>("codegeneration.codegen-datasources", "codegeneration.code_gen.datasource.title", "codegeneration.code_gen.datasource.fields.")
                .Reads(ListCodegenDatasources.Id, ListCodegenDatasourceOptions.Id, GetCodegenDatasource.Id)
                .Creates(CreateCodegenDatasource.Id)
                .Updates<UpdateCodegenDatasourceRequest>(UpdateCodegenDatasource.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysCodeGenDataSource>(context, request.Id, CodegenDatasourceDetailResponse.From))
                .Updates<SetCodegenDatasourceStatusRequest>(SetCodegenDatasourceStatus.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysCodeGenDataSource>(context, request.Id, CodegenDatasourceDetailResponse.From))
                .Keyword("sourceName", "sourceDescription", "host", "databaseName")
            .Entity<CodegenTableResponse>("codegeneration.codegen-tables", "codegeneration.code_gen.table.title", "codegeneration.code_gen.table.fields.")
                .Reads(ListCodegenTables.Id, ListCodegenTableOptions.Id)
                .ReadsAt(GetCodegenTable.Id, "table")
                .Updates<UpdateCodegenTableRequest>(UpdateCodegenTable.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysCodeGenTable>(context, request.Id, CodegenTableResponse.From))
                .Updates<SetCodegenTableStatusRequest>(SetCodegenTableStatus.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysCodeGenTable>(context, request.Id, CodegenTableResponse.From))
                .Keyword("tableName", "className", "tableComment")
            .Entity<CodegenTemplateResponse>("codegeneration.codegen-templates", "codegeneration.code_gen.template.title", "codegeneration.code_gen.template.fields.")
                .Reads(ListCodegenTemplates.Id)
                .ReadsAt(GetCodegenTemplate.Id, "template")
                .CreatesAt(CreateCodegenTemplate.Id, "template")
                .Updates<UpdateCodegenTemplateRequest>(UpdateCodegenTemplate.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysCodeGenTemplate>(context, request.Id, CodegenTemplateResponse.From), "template")
                .Updates<SetCodegenTemplateStatusRequest>(SetCodegenTemplateStatus.Id, static (request, context) => FieldSecurityOriginal.FindAsync<SysCodeGenTemplate>(context, request.Id, CodegenTemplateResponse.From), "template")
                .Keyword("templateCode", "templateName", "templateDescription"));
}
