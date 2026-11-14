// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Chat.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Chat.Operations.ChatCalls;

/// <summary>
/// 发起通话
/// </summary>
/// <remarks>
/// <c>POST /api/chat-calls:start</c>。只有单聊能发起；自己或对方正在别的通话里、对方不在线都被拒。成功即开始响铃，通话推给两个人的全部连接
/// </remarks>
/// <param name="calls">通话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatCallsCall)]
public sealed class StartChatCall(ChatCallService calls) : IOperation<StartChatCallRequest, ChatCallItem>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-calls.start";

    /// <inheritdoc />
    public async ValueTask<Result<ChatCallItem>> HandleAsync(StartChatCallRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await calls.StartAsync(context, me, request).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 接听通话
/// </summary>
/// <remarks>
/// <c>POST /api/chat-calls:accept</c>。只有受邀的人、还在响铃时能接；多端同时接只有先到的成功，其余端随推送停铃
/// </remarks>
/// <param name="calls">通话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatCallsCall)]
public sealed class AcceptChatCall(ChatCallService calls) : IOperation<ChatCallRequest, ChatCallItem>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-calls.accept";

    /// <inheritdoc />
    public async ValueTask<Result<ChatCallItem>> HandleAsync(ChatCallRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await calls.AcceptAsync(context, me, request.CallId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 拒绝通话
/// </summary>
/// <remarks>
/// <c>POST /api/chat-calls:reject</c>。只有受邀的人、还在响铃时能拒；通话随即结束并往会话里记一条
/// </remarks>
/// <param name="calls">通话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatCallsCall)]
public sealed class RejectChatCall(ChatCallService calls) : IOperation<ChatCallRequest, ChatCallItem>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-calls.reject";

    /// <inheritdoc />
    public async ValueTask<Result<ChatCallItem>> HandleAsync(ChatCallRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await calls.RejectAsync(context, me, request.CallId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 挂断通话
/// </summary>
/// <remarks>
/// <c>POST /api/chat-calls:hangup</c>。发起人在接通前挂断即取消（响铃到点了算未接听），接通后任一方挂断即结束；已经结束的照样成功、返回它最后的样子（两边可能同时挂）
/// </remarks>
/// <param name="calls">通话服务</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = ChatPermissions.ChatCallsCall)]
public sealed class HangupChatCall(ChatCallService calls) : IOperation<ChatCallRequest, ChatCallItem>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-calls.hangup";

    /// <inheritdoc />
    public async ValueTask<Result<ChatCallItem>> HandleAsync(ChatCallRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await calls.HangupAsync(context, me, request.CallId).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 我此刻所在的通话
/// </summary>
/// <remarks>
/// <c>GET /api/chat-calls:active</c>。响铃中或通话中的那一通，没有为空；页面刷新后据此认回来电
/// </remarks>
/// <param name="calls">通话服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatCallsRead)]
public sealed class GetActiveChatCall(ChatCallService calls) : IOperation<ChatCallEmptyRequest, ChatActiveCallResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-calls.active";

    /// <inheritdoc />
    public async ValueTask<Result<ChatActiveCallResponse>> HandleAsync(ChatCallEmptyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        return caller.TryGetValue(out var me) ? await calls.ActiveAsync(context, me).ConfigureAwait(false) : caller.Error!;
    }
}

/// <summary>
/// 通话用的 ICE 服务器
/// </summary>
/// <remarks>
/// <c>GET /api/chat-calls:ice-servers</c>。宿主配置里的 STUN 与 TURN；TURN 的凭据按调用方现签、到点失效，共享密钥不下发
/// </remarks>
/// <param name="calls">通话服务</param>
[Operation(Id, Stability = Stability.Preview, ReadOnly = true, Expose = Expose.Http, Permission = ChatPermissions.ChatCallsRead)]
public sealed class GetChatCallIceServers(ChatCallService calls) : IOperation<ChatCallEmptyRequest, ChatIceServersResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "chat-calls.ice-servers";

    /// <inheritdoc />
    public ValueTask<Result<ChatIceServersResponse>> HandleAsync(ChatCallEmptyRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var caller = ChatCaller.Of(context);
        Result<ChatIceServersResponse> result = caller.TryGetValue(out var me) ? calls.IceServers(me) : caller.Error!;
        return ValueTask.FromResult(result);
    }
}
