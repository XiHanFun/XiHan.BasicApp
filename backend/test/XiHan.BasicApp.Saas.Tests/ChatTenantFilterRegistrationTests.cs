// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.Framework.Core.Modularity;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.MultiTenancy;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 聊天实体严格租户过滤注册测试。
/// 验证应用在框架 NuGet 3.10.1 尚未提供严格租户标记时，仍按当前执行流隔离平台与租户聊天数据。
/// </summary>
public sealed class ChatTenantFilterRegistrationTests
{
    /// <summary>
    /// 验证 SaaS 模块为全部聊天实体注册具体类型过滤器，防止新增聊天表遗漏隔离策略。
    /// </summary>
    [Fact]
    public void ConfigureServices_ShouldRegisterFiltersForAllChatEntities()
    {
        var options = BuildSqlSugarOptions();
        var expectedEntityTypes = new[]
        {
            typeof(SysChatConversation),
            typeof(SysChatConversationMember),
            typeof(SysChatMessage),
            typeof(SysChatMessageReaction)
        };

        Assert.All(expectedEntityTypes, entityType => Assert.True(
            options.GlobalFilters.ContainsKey(entityType),
            $"缺少聊天实体 {entityType.Name} 的严格租户过滤器。"));
    }

    /// <summary>
    /// 验证过滤器在平台态只放行 TenantId=0，在租户态只放行当前租户。
    /// </summary>
    [Fact]
    public void StrictFilter_ShouldFollowCurrentTenantExecutionContext()
    {
        var options = BuildSqlSugarOptions();
        var filter = Assert.IsAssignableFrom<Expression<Func<SysChatConversation, bool>>>(
            options.GlobalFilters[typeof(SysChatConversation)]);
        var predicate = filter.Compile();

        Assert.True(predicate(new SysChatConversation { TenantId = 0 }));
        Assert.False(predicate(new SysChatConversation { TenantId = 42 }));

        var currentTenant = new CurrentTenant(AsyncLocalCurrentTenantAccessor.Instance);
        using (currentTenant.Change(42, "tenant-42"))
        {
            Assert.True(predicate(new SysChatConversation { TenantId = 42 }));
            Assert.False(predicate(new SysChatConversation { TenantId = 0 }));
            Assert.False(predicate(new SysChatConversation { TenantId = 43 }));
        }

        // Change 作用域释放后必须恢复平台态，避免测试泄漏 AsyncLocal 上下文并污染并行用例。
        Assert.True(predicate(new SysChatConversation { TenantId = 0 }));
    }

    /// <summary>
    /// 构造仅执行模块服务注册后的 SqlSugar 选项，不创建数据库连接。
    /// </summary>
    /// <returns>包含 SaaS 模块过滤配置的 SqlSugar 选项。</returns>
    private static XiHanSqlSugarCoreOptions BuildSqlSugarOptions()
    {
        var services = new ServiceCollection();
        var module = new XiHanBasicAppSaasModule();
        module.ConfigureServices(new ServiceConfigurationContext(services));

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<XiHanSqlSugarCoreOptions>>().Value;
    }
}
