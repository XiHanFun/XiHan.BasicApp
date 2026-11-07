// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Pages;

/// <summary>
/// 内置目录的节点编码
/// </summary>
/// <remarks>
/// 各包的页面登记用 <see cref="PageDescriptor.Directory"/> 指到这里的目录，目录节点本身由 Access 的内置节点种子首写，之后与运营建的一样可改可删，删了的目录下的页面留在顶层
/// 顶层是 AI 中心、流程中心、集成中心、系统管理、帮助中心五个目录，集成中心下再分推送服务与开放接口，系统管理下分权限、租户、设置、服务、监控、日志、文件、开发、消息九个二级目录
/// </remarks>
public static class MenuDirectories
{
    /// <summary>
    /// AI 中心
    /// </summary>
    public const string AiCenter = "custom.ai-center";

    /// <summary>
    /// 流程中心
    /// </summary>
    public const string WorkflowCenter = "custom.workflow-center";

    /// <summary>
    /// 集成中心
    /// </summary>
    public const string IntegrationCenter = "custom.integration-center";

    /// <summary>
    /// 集成中心 › 推送服务
    /// </summary>
    public const string PushService = "custom.push-service";

    /// <summary>
    /// 集成中心 › 开放接口
    /// </summary>
    public const string OpenApi = "custom.open-api";

    /// <summary>
    /// 系统管理
    /// </summary>
    public const string SystemManagement = "custom.system-management";

    /// <summary>
    /// 系统管理 › 系统权限
    /// </summary>
    public const string SystemPermission = "custom.system-permission";

    /// <summary>
    /// 系统管理 › 租户管理
    /// </summary>
    public const string TenantManagement = "custom.tenant-management";

    /// <summary>
    /// 系统管理 › 系统设置
    /// </summary>
    public const string SystemSettings = "custom.system-settings";

    /// <summary>
    /// 系统管理 › 系统服务
    /// </summary>
    public const string SystemServices = "custom.system-services";

    /// <summary>
    /// 系统管理 › 系统监控
    /// </summary>
    public const string SystemMonitor = "custom.system-monitor";

    /// <summary>
    /// 系统管理 › 系统日志
    /// </summary>
    public const string SystemLogs = "custom.system-logs";

    /// <summary>
    /// 系统管理 › 文件服务
    /// </summary>
    public const string FileServices = "custom.file-services";

    /// <summary>
    /// 系统管理 › 在线开发
    /// </summary>
    public const string OnlineDev = "custom.online-dev";

    /// <summary>
    /// 系统管理 › 消息中心
    /// </summary>
    public const string MessageCenter = "custom.message-center";

    /// <summary>
    /// 帮助中心
    /// </summary>
    public const string About = "custom.about";
}
