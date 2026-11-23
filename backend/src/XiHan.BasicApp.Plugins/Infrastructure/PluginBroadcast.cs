// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XiHan.Framework.Messaging.Abstractions.Envelopes;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Messaging.Abstractions.Publishing;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Plugins.Infrastructure;

/// <summary>
/// 插件改动的跨实例广播（单例）
/// </summary>
/// <remarks>
/// 方案 5.4：管理操作提交、本实例对齐之后发一条 <see cref="Topic"/>，其他实例收到后各自对齐（<see cref="PluginChangedHandler"/>）。消息不带内容，收到的实例重新读库
/// 单实例时是内存代理，配了 RabbitMQ 等代理才跨实例。广播是尽力而为：发不出只记日志，漏掉的变更由定时对齐补上（<see cref="PluginReconciler.RefreshAsync"/>）
/// </remarks>
/// <param name="scopes">作用域工厂</param>
/// <param name="logger">日志</param>
public sealed partial class PluginBroadcast(IServiceScopeFactory scopes, ILogger<PluginBroadcast> logger)
{
    /// <summary>
    /// 主题
    /// </summary>
    public const string Topic = "basicapp.plugins.changed";

    /// <summary>
    /// 本实例发出的信封来源：收到自己发的即跳过
    /// </summary>
    public string Source { get; } = "/basicapp/plugins/" + Guid.NewGuid().ToString("N");

    /// <summary>
    /// 告诉其他实例插件改了
    /// </summary>
    public async Task AnnounceAsync()
    {
        var envelope = new MessageEnvelope
        {
            Id = Guid.NewGuid().ToString("N"),
            Source = Source,
            Type = Topic,
            Data = "{}"u8.ToArray(),
        };
        await using var scope = scopes.CreateAsyncScope();
        var published = await scope.ServiceProvider.GetRequiredService<IMessagePublisher>().PublishAsync(Topic, envelope, CancellationToken.None).ConfigureAwait(false);
        if (published.IsFailure)
        {
            LogPublishFailed(logger, published.Error!.Code);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Announcing the plugin change to other instances failed ({Code}); they catch up on their next scheduled reconciliation.")]
    private static partial void LogPublishFailed(ILogger logger, string code);
}

/// <summary>
/// 插件改动的广播订阅（每个实例各收到一份）
/// </summary>
/// <remarks>
/// 以广播方式订阅 <see cref="PluginBroadcast.Topic"/>：发出的实例已经对齐过，按信封来源跳过；其他实例重新读库对齐。对齐的失败记在各插件上，不算投递失败
/// </remarks>
/// <param name="broadcast">本实例的广播</param>
/// <param name="reconciler">插件对齐服务</param>
public sealed class PluginChangedHandler(PluginBroadcast broadcast, PluginReconciler reconciler) : IMessageHandler
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (!string.Equals(delivery.Envelope.Source, broadcast.Source, StringComparison.Ordinal))
        {
            await reconciler.ReconcileAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
