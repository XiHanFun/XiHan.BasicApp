// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.Realtime.Infrastructure;
using XiHan.Framework.Messaging.Abstractions.Handlers;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Realtime.Handlers;

/// <summary>
/// 断开连接的广播消息
/// </summary>
/// <param name="SessionIds">要断开的会话</param>
internal sealed record DisconnectRequest(IReadOnlyList<Guid> SessionIds);

/// <summary>
/// 断开连接的广播订阅（每个实例各收到一份）
/// </summary>
/// <remarks>
/// <see cref="IMessageHandler"/>，以广播方式订阅 <see cref="Topic"/>：吊销会话的实例在提交之后发布，各实例移出并断开这些会话在本实例上的连接
/// 发布的实例自己已经直接断开过本实例的连接，按信封来源跳过自己发出的这一份。消息解不开即抛出（投递失败，广播重试到上限后丢弃）
/// </remarks>
/// <param name="node">本实例的标识</param>
/// <param name="closer">移出与断开连接</param>
internal sealed class DisconnectHandler(RealtimeNode node, ConnectionCloser closer) : IMessageHandler
{
    /// <summary>
    /// 主题
    /// </summary>
    public const string Topic = "basicapp.realtime.disconnect";

    /// <summary>
    /// 本实例发出的信封来源
    /// </summary>
    public static string SourceOf(RealtimeNode node) => "/basicapp/realtime/" + node.Id;

    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(MessageDelivery delivery, InvocationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(delivery);
        if (string.Equals(delivery.Envelope.Source, SourceOf(node), StringComparison.Ordinal))
        {
            return Result.Success();
        }

        var request = JsonSerializer.Deserialize(delivery.Envelope.Data.Span, RealtimeJsonContext.Default.DisconnectRequest)
            ?? throw new InvalidOperationException($"Message '{delivery.Envelope.Id}' on '{Topic}' has no data.");
        await closer.CloseLocalAsync(request.SessionIds).ConfigureAwait(false);
        return Result.Success();
    }
}
