// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Runtime.Middleware;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.BasicApp.Runtime.MultiTenancy;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;

namespace XiHan.BasicApp.Runtime;

/// <summary>
/// 运行时基座能力包（Tier 0）：在 Transaction 阶段把当前主体交给 <see cref="Persistence.AuditorStamp"/>，供 SqlSugar 回调填操作人字段；提供 <c>basicapp.runtime@1</c> 供各能力包依赖。
/// </summary>
public sealed class RuntimeFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "runtime",
        Package = "XiHan.BasicApp.Runtime",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Pipeline = [new PipelineContribution(typeof(AuditorStampMiddleware).FullName!, PipelineStage.Transaction)],
        },
        Entry = typeof(RuntimeFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddSingleton(SeedOptions.FromConfiguration(context.Configuration));
        context.Services.AddSingleton<SeedRunner>();
        context.Services.AddSingleton<ITenantScopeRunner, TenantScopeRunner>();
        // 持久化 Provider 的工作单元中间件也在 Transaction 阶段；本包依赖 persistence.relational@1，按拓扑序排在它之后
        context.AddMiddleware<AuditorStampMiddleware>(PipelineStage.Transaction);
    }
}
