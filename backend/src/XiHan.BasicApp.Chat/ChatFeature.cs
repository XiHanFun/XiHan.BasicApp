// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.BasicApp.Chat.Handlers;
using XiHan.BasicApp.Chat.Infrastructure;
using XiHan.BasicApp.Chat.Jobs;
using XiHan.BasicApp.Chat.Queries;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Realtime;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Realtime;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.BasicApp.Runtime;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.Messaging.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Scheduling.Abstractions;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Chat;

/// <summary>
/// 在线聊天模块（Tier 3，可选）
/// </summary>
/// <remarks>
/// 会话、成员、消息与表情回应四张租户库表（平台一侧租户为 <c>platform</c>），聊天 Hub <see cref="RealtimeHubs.Chat"/> 由本包登记、宿主一并映射，
/// 推送经 Realtime 在提交之后发出。部门群随 Organization 的领域事件 <see cref="Topics.OrganizationMembershipChanged"/> 同步成员，在线状态随 Realtime 的 <see cref="Topics.RealtimePresenceChanged"/> 推给相关的人；
/// 敏感词与保留天数读参数 <c>chat.policy</c>，保留清理是内建作业 <c>chat.messages.cleanup</c>；附件以会话为访问范围（方案 H2，经 Files 的附件契约）；
/// 另补服务端转发（方案 H1）与审计的消息详情、会话成员（方案 H4）。助手经 Contracts 的 <c>IChatAssistants</c> 应答（AI 实现），AI 关掉时助手操作明确返回不可用，其余照常
/// 宿主按 <c>BasicApp:Modules:Chat:Enabled</c> 决定是否装配；关掉后权限目录、页面登记、Hub 与订阅都不在，菜单种子删掉它的页面
/// </remarks>
public sealed class ChatFeature : IFeature
{
    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "chat",
        Package = "XiHan.BasicApp.Chat",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(ChatCapabilities.Chat, ChatCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MessagingCapabilities.Queue, MessagingCapabilities.Version),
            new CapabilityRequirement(SchedulingCapabilities.Jobs, SchedulingCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(RealtimeCapabilities.Realtime, RealtimeCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = ChatPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(ChatFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(ChatMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<ChatStore>();
        context.Services.AddScoped<ChatTexts>();
        context.Services.AddScoped<ChatPolicy>();
        context.Services.AddScoped<ChatPush>();
        context.Services.AddScoped<ChatMessageService>();
        context.Services.AddScoped<ChatConversationService>();
        context.Services.AddScoped<ChatAssistantService>();
        context.Services.AddScoped<ChatQueries>();
        context.Services.AddScoped<ChatAuditQueries>();
        context.Services.AddRealtimeHub<ChatHub>(RealtimeHubs.Chat);
        // 部门群同步：订阅 Organization 的部门归属事件（竞争消费，按处理时的归属核对）
        context.Services.AddMessageHandler<MembershipChangedHandler>(Topics.OrganizationMembershipChanged);
        // 在线状态：订阅 Realtime 的上线与离线（竞争消费，按处理时的在线状态推）
        context.Services.AddMessageHandler<PresenceChangedHandler>(Topics.RealtimePresenceChanged);
        context.Services.AddJob<ChatRetentionJob>(new JobName(ChatRetentionJob.Name), ChatRetentionJob.Schedule);
        context.Services.AddPermissionCatalog("chat", ChatPermissions.All);
        context.Services.AddPageCatalog("chat", ChatPages.All);
        context.Services.AddConfigCatalog("chat", [ChatPolicy.Config]);
        foreach (var permission in ChatPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }
}
