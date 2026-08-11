// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Builder;
using XiHan.BasicApp.Chat.Extensions;
using XiHan.BasicApp.Chat.Hubs;
using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Application;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Web.Core.Extensions;
using XiHan.Framework.Web.RealTime.Constants;
using XiHan.Framework.Web.RealTime.Extensions;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 曦寒基础应用在线聊天模块
/// </summary>
/// <remarks>
/// 独立一等模块（与 AI/代码生成/工作流/打印同构）：单聊/群聊/部门群/AI 助手会话、
/// 消息与表情回应、SignalR 实时推送、敏感词拦截、保留期清理与管理侧合规审计。
/// 依赖 Saas（复用 RBAC 表、SaasRepository、组织事件、菜单/权限种子基类）。
/// </remarks>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppChatModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;

        // 种子（权限 → 菜单 → 角色授权 → 任务 → 配置）+ 领域服务 + 事件处理器
        services.AddChatDataSeeders();
        services.AddChatDomainServices();
        services.AddChatEventHandlers();
    }

    /// <summary>
    /// 应用初始化：映射聊天 SignalR Hub 端点
    /// </summary>
    /// <param name="context"></param>
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        app.UseEndpoints(endpoints => endpoints.MapXiHanHub<BasicAppChatHub>(SignalRConstants.HubPaths.Chat));
    }
}
