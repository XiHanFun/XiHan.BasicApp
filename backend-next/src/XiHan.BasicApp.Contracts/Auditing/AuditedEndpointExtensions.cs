// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Auditing;

/// <summary>
/// 普通端点在调用上下文里登记自己的身份。
/// </summary>
/// <remarks>
/// 普通端点（上传、下载等不能用操作表达的端点）在调用上下文里登记自己的身份，访问日志与数据变更日志据此记录（操作调用按操作 ID 记录，不用这里）。
/// 宿主的端点过滤器在端点执行时登记身份、执行后登记实际的 HTTP 状态码；授权阶段就被拒绝的调用不会走到端点，没有这条登记。
/// </remarks>
public static class AuditedEndpointExtensions
{
    private static readonly ContextItemKey<AuditedEndpoint> _endpointKey = new("basicapp.audit.endpoint");

    /// <summary>
    /// 登记当前调用是哪个普通端点。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="id">端点身份（与操作 ID 同一种写法，如 <c>files.upload</c>）。</param>
    public static void SetAuditedEndpoint(this InvocationContext context, string id)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        context.Set(_endpointKey, new AuditedEndpoint(id));
    }

    /// <summary>
    /// 登记端点实际写出的 HTTP 状态码。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <param name="statusCode">状态码。</param>
    public static void SetAuditedEndpointStatus(this InvocationContext context, int statusCode)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.Get(_endpointKey).TryGetValue(out var endpoint))
        {
            endpoint.StatusCode = statusCode;
        }
    }

    /// <summary>
    /// 取当前调用登记的普通端点。
    /// </summary>
    /// <param name="context">调用上下文。</param>
    /// <returns>登记的端点；不是普通端点（或没有登记）为无值。</returns>
    public static Maybe<AuditedEndpoint> GetAuditedEndpoint(this InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Get(_endpointKey);
    }
}

/// <summary>
/// 登记的普通端点。
/// </summary>
/// <param name="id">端点身份。</param>
public sealed class AuditedEndpoint(string id)
{
    /// <summary>
    /// 端点身份（与操作 ID 同一种写法）。
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// 端点实际写出的 HTTP 状态码。
    /// </summary>
    /// <remarks>
    /// 端点没执行完（异常、失败结果）时为空。
    /// </remarks>
    public int? StatusCode { get; set; }
}
