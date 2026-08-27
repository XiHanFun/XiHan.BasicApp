// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.AI.Domain.DomainServices;
using XiHan.BasicApp.AI.Domain.DomainServices.Implementations;
using XiHan.BasicApp.AI.Extensions;
using XiHan.BasicApp.AI.Infrastructure.Configuration;
using XiHan.BasicApp.AI.Infrastructure.Security;
using XiHan.BasicApp.AI.Infrastructure.Seeders.System;
using XiHan.BasicApp.AI.Infrastructure.Skills;
using XiHan.Framework.AI.Abstractions.Configuration;
using XiHan.Framework.AI.Abstractions.Prompts;
using XiHan.Framework.AI.Abstractions.Skills;
using XiHan.Framework.Data.SqlSugar.Seeders;

namespace XiHan.BasicApp.AI.Tests;

/// <summary>
/// AI 模块服务注册的契约测试：覆盖"必须 Replace 而不是 TryAdd"这条回归锚点、
/// 各服务的生命周期，以及四段种子器的登记完整性。
/// </summary>
/// <remarks>
/// 只在 <see cref="IServiceCollection"/> 上做登记与断言，不 Build 容器、不解析实例，
/// 因此不会触碰数据库、向量库或任何外部依赖。
/// </remarks>
public sealed class AiServiceRegistrationTests
{
    /// <summary>
    /// provider 配置源必须以 Replace 覆盖框架默认实现：框架已用 TryAddSingleton 占位，
    /// 这里若改成 TryAdd 会被静默忽略，DB 里配的 provider 永远不生效。
    /// </summary>
    [Fact]
    public void AddAIConfigStore_ShouldReplaceFrameworkDefaultInsteadOfBeingIgnored()
    {
        var services = new ServiceCollection();
        services.TryAddSingleton<IAiProviderConfigStore, FrameworkDefaultProviderConfigStore>();

        _ = services.AddAIConfigStore();

        var descriptor = Assert.Single(services, item => item.ServiceType == typeof(IAiProviderConfigStore));
        Assert.Equal(typeof(SaasAiProviderConfigStore), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    /// 提示词库同样必须以 Replace 覆盖框架默认实现，否则库里维护的提示词永远读不到。
    /// </summary>
    [Fact]
    public void AddPromptStore_ShouldReplaceFrameworkDefaultInsteadOfBeingIgnored()
    {
        var services = new ServiceCollection();
        services.TryAddSingleton<IAiPromptStore, FrameworkDefaultPromptStore>();

        _ = services.AddPromptStore();

        var descriptor = Assert.Single(services, item => item.ServiceType == typeof(IAiPromptStore));
        Assert.Equal(typeof(SaasAiPromptStore), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    /// 领域服务无 DI 标记接口，框架不会自动注册，必须显式登记为 Scoped（与请求内的工作单元同生命周期）。
    /// </summary>
    [Fact]
    public void AddDomainServices_ShouldRegisterEveryDomainServiceAsScoped()
    {
        var services = new ServiceCollection();

        _ = services.AddAIDomainServices();
        _ = services.AddRAGDomainServices();
        _ = services.AddPromptDomainServices();
        _ = services.AddAssistantDomainServices();

        AssertRegistered(services, typeof(IAiProviderDomainService), typeof(AiProviderDomainService), ServiceLifetime.Scoped);
        AssertRegistered(services, typeof(IKnowledgeDocumentDomainService), typeof(KnowledgeDocumentDomainService), ServiceLifetime.Scoped);
        AssertRegistered(services, typeof(IAiPromptDomainService), typeof(AiPromptDomainService), ServiceLifetime.Scoped);
        AssertRegistered(services, typeof(IAiAssistantDomainService), typeof(AiAssistantDomainService), ServiceLifetime.Scoped);
    }

    /// <summary>
    /// 密钥保护器无状态、依赖单例的 Data Protection 提供者，必须注册为 Singleton；
    /// 降成 Scoped 会在每个请求里重新申领保护器，白白增加开销。
    /// </summary>
    [Fact]
    public void AddAIDomainServices_ShouldRegisterSecretProtectorAsSingleton()
    {
        var services = new ServiceCollection();

        _ = services.AddAIDomainServices();

        AssertRegistered(
            services,
            typeof(IAiProviderSecretProtector),
            typeof(DataProtectionAiProviderSecretProtector),
            ServiceLifetime.Singleton);
    }

    /// <summary>
    /// 检索技能必须以 <see cref="IAiSkill"/> 登记为 Singleton，框架技能注册表构造时按该契约收纳。
    /// </summary>
    [Fact]
    public void AddAISkills_ShouldRegisterKnowledgeSkillAsSingletonUnderSkillContract()
    {
        var services = new ServiceCollection();

        _ = services.AddAISkills();

        var descriptor = Assert.Single(services, item => item.ServiceType == typeof(IAiSkill));
        Assert.Equal(typeof(KnowledgeRetrieveSkill), descriptor.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    /// 四段种子器必须全部登记到 <see cref="IDataSeeder"/> 上，漏登记的种子器永远不会执行。
    /// </summary>
    [Fact]
    public void AddDataSeeders_ShouldRegisterEverySeederInTheModule()
    {
        var services = new ServiceCollection();

        _ = services.AddAIDataSeeders();
        _ = services.AddRAGDataSeeders();
        _ = services.AddPromptDataSeeders();
        _ = services.AddAssistantDataSeeders();

        var registered = services
            .Where(item => item.ServiceType == typeof(IDataSeeder))
            .Select(item => item.ImplementationType!.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        var expected = new[]
        {
            nameof(AiMenuSeeder),
            nameof(AssistantPermissionSeeder),
            nameof(AssistantResourceSeeder),
            nameof(AssistantRolePermissionSeeder),
            nameof(KnowledgePermissionSeeder),
            nameof(KnowledgeResourceSeeder),
            nameof(KnowledgeRolePermissionSeeder),
            nameof(PromptPermissionSeeder),
            nameof(PromptResourceSeeder),
            nameof(PromptRolePermissionSeeder),
            nameof(SysOperationSeeder),
            nameof(SysPermissionSeeder),
            nameof(SysResourceSeeder),
            nameof(SysRolePermissionSeeder)
        }.OrderBy(name => name, StringComparer.Ordinal).ToList();

        Assert.Equal(expected, registered, StringComparer.Ordinal);
    }

    /// <summary>
    /// 种子器一律登记为 Scoped：它们持有作用域内的数据库客户端，登记成 Singleton 会跨请求复用连接。
    /// </summary>
    [Fact]
    public void AddDataSeeders_ShouldRegisterSeedersAsScoped()
    {
        var services = new ServiceCollection();

        _ = services.AddAIDataSeeders();

        var lifetimes = services
            .Where(item => item.ServiceType == typeof(IDataSeeder))
            .Select(item => item.Lifetime)
            .Distinct()
            .ToList();

        var lifetime = Assert.Single(lifetimes);
        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    /// <summary>
    /// 重复调用注册扩展不得把同一个种子器登记两遍，否则同一份种子会被执行两次。
    /// </summary>
    [Fact]
    public void AddAIDataSeeders_CalledOnceShouldRegisterEachSeederExactlyOnce()
    {
        var services = new ServiceCollection();

        _ = services.AddAIDataSeeders();

        var duplicated = services
            .Where(item => item.ServiceType == typeof(IDataSeeder))
            .GroupBy(item => item.ImplementationType!)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.Name)
            .ToList();

        Assert.True(duplicated.Count == 0, $"下列种子器被重复登记：{string.Join("、", duplicated)}。");
    }

    /// <summary>
    /// RAG 装配依赖配置对象，空配置必须 fail-fast 而不是拿默认值去连一个不存在的向量库。
    /// </summary>
    [Fact]
    public void AddRAG_NullConfigurationShouldReject()
    {
        var services = new ServiceCollection();

        _ = Assert.Throws<ArgumentNullException>(() => services.AddRAG(null!));
    }

    /// <summary>
    /// AI 模块必须显式依赖 Saas 模块：RBAC 表、仓储基建与密文前缀都从那里来，去掉依赖会在启动期才炸。
    /// </summary>
    [Fact]
    public void Module_ShouldDependOnSaasModule()
    {
        var dependsOn = typeof(XiHanBasicAppAIModule)
            .GetCustomAttributes(typeof(XiHan.Framework.Core.Modularity.DependsOnAttribute), inherit: false)
            .Cast<XiHan.Framework.Core.Modularity.DependsOnAttribute>()
            .SelectMany(attribute => attribute.GetDependedTypes())
            .ToList();

        Assert.Contains(typeof(XiHan.BasicApp.Saas.XiHanBasicAppSaasModule), dependsOn);
    }

    /// <summary>
    /// 断言某个契约恰好登记了一次，且实现类型与生命周期符合预期。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="serviceType">契约类型。</param>
    /// <param name="implementationType">期望的实现类型。</param>
    /// <param name="lifetime">期望的生命周期。</param>
    private static void AssertRegistered(
        IServiceCollection services,
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        var descriptor = Assert.Single(services, item => item.ServiceType == serviceType);

        Assert.Equal(implementationType, descriptor.ImplementationType);
        Assert.Equal(lifetime, descriptor.Lifetime);
    }

    /// <summary>
    /// 模拟框架默认的 provider 配置源（用于验证 Replace 语义确实覆盖了它）。
    /// </summary>
    private sealed class FrameworkDefaultProviderConfigStore : IAiProviderConfigStore
    {
        /// <summary>
        /// 取指定 provider 配置（测试替身，恒为 null）。
        /// </summary>
        /// <param name="providerName">provider 名。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>恒为 null。</returns>
        public Task<AiProviderOptions?> GetAsync(string? providerName = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AiProviderOptions?>(null);
        }

        /// <summary>
        /// 取全部 provider 配置（测试替身，恒为空集合）。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>空集合。</returns>
        public Task<IReadOnlyList<AiProviderOptions>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AiProviderOptions>>([]);
        }
    }

    /// <summary>
    /// 模拟框架默认的提示词库（用于验证 Replace 语义确实覆盖了它）。
    /// </summary>
    private sealed class FrameworkDefaultPromptStore : IAiPromptStore
    {
        /// <summary>
        /// 取模板（测试替身，恒为 null）。
        /// </summary>
        /// <param name="name">模板名。</param>
        /// <param name="version">版本。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>恒为 null。</returns>
        public Task<AiPromptTemplate?> GetAsync(string name, string? version = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AiPromptTemplate?>(null);
        }

        /// <summary>
        /// 列出模板（测试替身，恒为空集合）。
        /// </summary>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>空集合。</returns>
        public Task<IReadOnlyList<AiPromptTemplate>> ListAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AiPromptTemplate>>([]);
        }
    }
}
