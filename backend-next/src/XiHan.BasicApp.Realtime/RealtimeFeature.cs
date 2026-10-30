// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.BasicApp.Realtime.Middleware;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Hosting.AspNetCore.Authentication;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Realtime;

/// <summary>
/// 实时能力包（Tier 2）。
/// </summary>
/// <remarks>
/// SignalR 通知 Hub（<see cref="RealtimeHubs.Notification"/>，由宿主经 <see cref="RealtimeEndpoints.MapRealtimeHubs"/> 映射）、
/// 连接登记与在线状态（<see cref="IRealtimePresence"/>）、推送（<see cref="IRealtimePush"/>，调用成功提交后才发出）。没有操作与权限：推送由各包在自己的操作里发起。
/// </remarks>
public sealed class RealtimeFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "realtime",
        Package = "XiHan.BasicApp.Realtime",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(RealtimeCapabilities.Realtime, RealtimeCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authentication, SecurityCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Pipeline = [new PipelineContribution(typeof(RealtimeDispatchMiddleware).FullName!, PipelineStage.Transaction)],
        },
        Entry = typeof(RealtimeFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddSignalR();
        // Hub 绕过内核管道：认证交给框架的 XiHan 方案（与内核同一套令牌校验，ADR-0051），浏览器的 WebSocket / EventSource 只在 Hub 路径下认查询串令牌；会话门禁由过滤器做
        context.Services.AddAuthentication().AddXiHan(static options => options.QueryTokenPaths.Add(RealtimeHubs.Prefix));
        context.Services.AddSingleton<IInvocationAuthenticationFilter, SessionAuthenticationFilter>();
        context.Services.AddSingleton<ConnectionRegistry>();
        context.Services.AddSingleton<IRealtimePresence>(static services => services.GetRequiredService<ConnectionRegistry>());
        context.Services.AddScoped<RealtimeOutbox>();
        context.Services.AddScoped<IRealtimePush>(static services => services.GetRequiredService<RealtimeOutbox>());
        // 排在 Transaction 阶段开头、工作单元之外：工作单元提交之后它才拿到结果
        context.AddMiddleware<RealtimeDispatchMiddleware>(PipelineStage.Transaction, Placement.Beginning);
    }
}
