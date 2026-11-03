// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Workflow.Operations.WorkflowDefinitions;
using XiHan.BasicApp.Workflow.Operations.WorkflowInstances;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 工作流的字段安全登记
/// </summary>
/// <remarks>
/// 照 4.x 登记定义与实例，但只登记读的部分（设计稿 D9）：定义与实例存在框架的引擎表里，6.0 没有应用实体，写入的原值取不到；
/// 读的打码与关键字门控照常由 Access 统一套用
/// </remarks>
public static class WorkflowFieldSecurity
{
    /// <summary>
    /// 登记
    /// </summary>
    /// <param name="services">服务集合</param>
    public static void Register(IServiceCollection services)
        => services.AddFieldSecurity("workflow", WorkflowJsonContext.Default, fields => fields
            .Entity<WorkflowDefinitionDetailResponse>("workflow.workflow-definitions", "workflow.definitions.title", "workflow.definitions.fields.")
                .Reads(ListWorkflowDefinitions.Id, GetWorkflowDefinition.Id)
                .Keyword("name", "displayName", "description")
            .Entity<WorkflowInstanceResponse>("workflow.workflow-instances", "workflow.instances.title", "workflow.instances.fields.")
                .Reads(ListWorkflowInstances.Id)
                .Keyword("title", "correlationId"));
}
