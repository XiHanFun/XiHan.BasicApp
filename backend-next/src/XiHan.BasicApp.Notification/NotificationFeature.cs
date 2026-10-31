// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Caching;
using XiHan.BasicApp.Contracts.Notification;
using XiHan.BasicApp.Contracts.Pages;
using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Workbench;
using XiHan.BasicApp.Notification.Domain.Services;
using XiHan.BasicApp.Notification.Infrastructure;
using XiHan.BasicApp.Notification.Infrastructure.Telegram;
using XiHan.BasicApp.Notification.Queries;
using XiHan.BasicApp.Notification.Seeds;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Runtime.Seeding;
using XiHan.Framework.Caching.Abstractions;
using XiHan.Framework.Kernel;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Localization.Abstractions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Primitives;
using XiHan.Framework.Primitives.Manifests;
using XiHan.Framework.Security.Abstractions;

namespace XiHan.BasicApp.Notification;

/// <summary>
/// 通知能力包（Tier 2）。
/// </summary>
/// <remarks>
/// 通知公告（租户库实体，平台一侧租户为 <c>platform</c>）与收件箱。接收人经 Contracts 的 <see cref="Contracts.Users.IRecipientSource"/> 解析
/// （Identity、Access、Organization 实现），推送经 <see cref="Contracts.Realtime.IRealtimePush"/>。
/// 邮件与短信：消息模板、渠道配置（平台库实体，随所属侧）与发送记录，经 Contracts 的 <see cref="IMessageSender"/> 入队，由后台循环（<see cref="MessageDispatcher"/>）发出；钉钉、飞书与企业微信的群机器人按本侧每个服务商的缺省机器人广播，平台一侧另发到各 Telegram 机器人的广播群，同一个循环发出。
/// Telegram 机器人的交互（平台托管）：<see cref="TelegramBotManager"/> 按平台设置走 Webhook（宿主映射 <see cref="TelegramWebhookEndpoints"/>）或长轮询，
/// <see cref="TelegramDispatcher"/> 按 4.x 的顺序路由到命令、回调、内联查询等处理器（内置 <c>/start</c> <c>/help</c> <c>/myid</c>），出站经 <see cref="TelegramNotifier"/> 重试并审计。
/// </remarks>
public sealed class NotificationFeature : IHostedFeature, IDisposable
{
    private CancellationTokenSource? _stopping;
    private Task? _loop;
    private Task? _telegram;

    /// <inheritdoc />
    public Manifest Manifest { get; } = new()
    {
        Id = AppInfo.ManifestIdPrefix + "notification",
        Package = "XiHan.BasicApp.Notification",
        Version = AppInfo.Version,
        Primitives = 1,
        Stability = Stability.Preview,
        Kind = ManifestKind.Capability,
        Provides = [new CapabilityDeclaration(NotificationCapabilities.Notification, NotificationCapabilities.Version)],
        Requires =
        [
            new CapabilityRequirement(KernelCapabilities.Hosting, KernelCapabilities.Version),
            new CapabilityRequirement(PersistenceCapabilities.Relational, PersistenceCapabilities.Version),
            new CapabilityRequirement(SecurityCapabilities.Authorization, SecurityCapabilities.Version),
            new CapabilityRequirement(MultiTenancyCapabilities.Context, MultiTenancyCapabilities.Version),
            new CapabilityRequirement(RuntimeCapabilities.Runtime, RuntimeCapabilities.Version),
            new CapabilityRequirement(CachingCapabilities.Distributed, CachingCapabilities.Version),
        ],
        Contributes = new Contributions
        {
            Permissions = NotificationPermissions.All.Select(static p => p.Code).ToList(),
        },
        Entry = typeof(NotificationFeature).FullName,
    };

    /// <inheritdoc />
    public void Configure(FeatureContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Services.AddMessageCatalog(NotificationMessages.Catalog);
        context.Services.AddGeneratedRepositories();
        context.Services.AddScoped<NotificationService>();
        context.Services.AddScoped<InboxService>();
        context.Services.AddScoped<NotificationQueries>();
        context.Services.AddScoped<InboxQueries>();
        context.Services.AddSingleton(static services => NotificationOptions.FromConfiguration(services.GetRequiredService<IConfiguration>()));
        context.Services.AddHttpClient();
        context.Services.AddScoped<MessageTemplateService>();
        context.Services.AddScoped<ChannelConfigService>();
        context.Services.AddScoped<MessageRecordService>();
        context.Services.AddScoped<MessagingQueries>();
        context.Services.AddScoped<IWorkbenchWidgetProvider, NotificationWidgets>();
        context.Services.AddScoped<BotConfigService>();
        context.Services.AddScoped<TelegramBotService>();
        context.Services.AddScoped<BotBroadcaster>();
        context.Services.AddSingleton<MessageDispatchSignal>();
        context.Services.AddScoped<IMessageSender, MessageSender>();
        // 测试与宿主可先登记自己的通道替换真实的 SMTP / 云厂商
        context.Services.TryAddSingleton<IEmailTransport, SmtpEmailTransport>();
        context.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ISmsTransport, AliyunSmsTransport>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ISmsTransport, TencentSmsTransport>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBotTransport, DingTalkBotTransport>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBotTransport, LarkBotTransport>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBotTransport, WeComBotTransport>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IBotTransport, TelegramBotTransport>());
        context.Services.AddSingleton(static services => TelegramOptions.FromConfiguration(services.GetRequiredService<IConfiguration>()));
        context.Services.TryAddSingleton<ITelegramHttp, TelegramHttp>();
        context.Services.AddSingleton<TelegramApi>();
        context.Services.AddSingleton<TelegramRuntime>();
        context.Services.AddSingleton<TelegramNotifier>();
        context.Services.AddSingleton<TelegramDispatcher>();
        context.Services.AddSingleton<TelegramBotManager>();
        context.Services.AddScoped<TelegramConversationStore>();
        context.Services.TryAddEnumerable(ServiceDescriptor.Scoped<ITelegramCommandHandler, StartCommandHandler>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Scoped<ITelegramCommandHandler, HelpCommandHandler>());
        context.Services.TryAddEnumerable(ServiceDescriptor.Scoped<ITelegramCommandHandler, MyIdCommandHandler>());
        context.Services.AddSingleton<MessageDispatcher>();
        context.Services.AddSeeder<MessageTemplateSeeder>();
        context.Services.AddPermissionCatalog("notification", NotificationPermissions.All);
        // 缓存管理只开放登记过的命名空间
        context.Services.AddCacheNamespace("notification", editable: true);
        context.Services.AddPageCatalog("notification", NotificationPages.All);
        foreach (var permission in NotificationPermissions.All)
        {
            context.AddPermission(permission.ToDefinition());
        }

        context.AddGeneratedOperations();
    }

    /// <inheritdoc />
    public async Task StartAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        var dispatcher = services.GetRequiredService<MessageDispatcher>();
        // 上个进程发到一半的记录改回排队中
        await dispatcher.RecoverAsync(cancellationToken).ConfigureAwait(false);
        _stopping = new CancellationTokenSource();
        _loop = dispatcher.RunAsync(_stopping.Token);

        // 处理器登记有误（命令或 action 重复、正则写错）启动即报错（4.x 同）
        await using (var scope = services.CreateAsyncScope())
        {
            _ = new TelegramCatalog(
                scope.ServiceProvider.GetServices<ITelegramCommandHandler>(),
                scope.ServiceProvider.GetServices<ITelegramCallbackHandler>(),
                scope.ServiceProvider.GetServices<ITelegramInlineQueryHandler>(),
                scope.ServiceProvider.GetServices<ITelegramStartPayloadHandler>(),
                scope.ServiceProvider.GetServices<ITelegramReplyHandler>(),
                scope.ServiceProvider.GetServices<ITelegramMessageHandler>(),
                scope.ServiceProvider.GetServices<ITelegramStateHandler>());
        }

        _telegram = services.GetRequiredService<TelegramBotManager>().RunAsync(_stopping.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stopping is null || _loop is null)
        {
            return;
        }

        await _stopping.CancelAsync().ConfigureAwait(false);
        await _loop.ConfigureAwait(false);
        if (_telegram is not null)
        {
            await _telegram.ConfigureAwait(false);
            _telegram = null;
        }

        _stopping.Dispose();
        _stopping = null;
        _loop = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _stopping?.Dispose();
        _stopping = null;
    }
}
