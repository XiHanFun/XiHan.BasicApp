// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace CrmPlugin;

/// <summary>
/// 只接受带本次会话令牌的调用
/// </summary>
/// <remarks>
/// 宿主的每一次调用都在元数据 <c>xihan-plugin-session</c> 里带启动时下发的 <c>XIHAN_PLUGIN_SESSION</c>：恰好一个值且相符才放行，按常数时间比较
/// </remarks>
internal sealed class SessionGuard : Interceptor
{
    /// <summary>
    /// 会话令牌的元数据键
    /// </summary>
    private const string SessionMetadataKey = "xihan-plugin-session";

    /// <summary>
    /// 本次进程的会话令牌
    /// </summary>
    private readonly byte[] _session = Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("XIHAN_PLUGIN_SESSION") ?? string.Empty);

    /// <inheritdoc />
    public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);
        Require(context);
        return continuation(request, context);
    }

    /// <inheritdoc />
    public override Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);
        Require(context);
        return continuation(request, responseStream, context);
    }

    /// <inheritdoc />
    public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);
        Require(context);
        return continuation(requestStream, context);
    }

    /// <inheritdoc />
    public override Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);
        Require(context);
        return continuation(requestStream, responseStream, context);
    }

    /// <summary>
    /// 核对令牌，不符即以 <c>Unauthenticated</c> 拒绝
    /// </summary>
    private void Require(ServerCallContext context)
    {
        // 两个值里一个对一个错也不放行，所以先数个数；没有令牌的环境（直接运行插件）一律拒绝
        var values = context.RequestHeaders.Where(static entry => !entry.IsBinary && string.Equals(entry.Key, SessionMetadataKey, StringComparison.OrdinalIgnoreCase)).Select(static entry => entry.Value).ToList();
        var presented = values.Count == 1 ? values[0] : null;
        if (_session.Length == 0 || presented is null || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), _session))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, "The plugin session token is missing or invalid."));
        }
    }
}
