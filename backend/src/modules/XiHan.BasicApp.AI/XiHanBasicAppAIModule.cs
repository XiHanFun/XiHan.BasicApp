#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanBasicAppAIModule
// Guid:a11c0de0-0001-4a10-9a00-00000000ai01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 14:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.AI.Extensions;
using XiHan.BasicApp.Saas;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Modularity;

namespace XiHan.BasicApp.AI;

/// <summary>
/// 曦寒基础应用 AI 应用模块
/// </summary>
/// <remarks>
/// 应用层 AI 模块（与代码生成模块同构，独立工程 XiHan.BasicApp.AI）。
/// 坐落在框架薄层 <c>XiHan.Framework.AI</c> 之上：框架给 provider 解析/会话门面，本模块给
/// provider 的库化管理（SysAiProvider 实体 + 加密 ApiKey + CRUD + 覆盖框架默认配置源 + 权限/菜单/种子）。
/// 依赖 Saas（复用 RBAC 表、SaasRepository、DataProtection 密文前缀）；框架 AI 经 Saas→Core 传递依赖。
/// </remarks>
[DependsOn(
    typeof(XiHanBasicAppSaasModule)
)]
public class XiHanBasicAppAIModule : XiHanModule
{
    /// <summary>
    /// 服务配置
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        var configuration = services.GetConfiguration();

        // AI Provider 配置管理：种子（操作 → 资源 → 权限 → 菜单 → 角色授权）+ 领域服务 + DB 配置源覆盖
        services.AddAIDataSeeders();
        services.AddAIDomainServices();
        services.AddAIConfigStore();

        // 知识库（RAG）：种子（资源 → 权限 → 菜单 → 角色授权）+ 领域服务 + 框架 RAG + Qdrant 向量库连接器
        services.AddRAGDataSeeders();
        services.AddRAGDomainServices();
        services.AddRAG(configuration);

        // AI 技能（注册为 IAiSkill，框架技能注册表收纳 → 对话工具 / MCP tools）
        services.AddAISkills();
    }
}
