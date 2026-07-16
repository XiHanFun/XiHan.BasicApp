#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppWorkflowModule
// Guid:96ee036c-a913-44b7-a94b-e0fb80d3f1f4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/16 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
