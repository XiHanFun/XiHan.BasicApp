// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas;
using XiHan.BasicApp.Workflow.Extensions;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Workflow;

namespace XiHan.BasicApp.Workflow;

/// <summary>
/// 曦寒基础应用工作流应用模块
/// </summary>
/// <remarks>
/// 在框架工作流引擎（XiHanWorkflowModule）之上提供：SqlSugar 持久化存储（Replace 内存默认）、
/// 定义/实例/待办应用服务、权限与菜单种子、待办站内通知。
/// </remarks>
[DependsOn(
    typeof(XiHanBasicAppSaasModule),
    typeof(XiHanWorkflowModule)
)]
public class XiHanBasicAppWorkflowModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 仓储与应用服务由框架约定扫描自动注册；此处登记存储替换、种子与事件处理器
        services.AddWorkflowStores();
        services.AddWorkflowDataSeeders();
        services.AddWorkflowEventHandlers();
    }
}
