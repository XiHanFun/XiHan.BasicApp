// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Auditing;
using XiHan.Framework.Auditing.Abstractions.Pipeline;
using XiHan.Framework.Hosting.AspNetCore.Endpoints;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Host.Endpoints;

/// <summary>
/// 普通端点的审计过滤器。
/// </summary>
/// <remarks>
/// 端点执行前在调用上下文里登记端点身份（访问日志、数据变更日志按它记录），写入类端点同时登记操作审计动作；
/// 端点返回的结果执行完后登记实际写出的状态码。授权阶段就被拒绝的调用到不了这里（框架只把操作描述与授权要求从端点元数据带进上下文）。
/// </remarks>
/// <param name="id">端点身份（与操作 ID 同一种写法）。</param>
/// <param name="audited">是否记操作审计（写入类端点）。</param>
internal sealed class AuditedEndpointFilter(string id, bool audited) : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
        var http = context.HttpContext;
        if (http.GetXiHanContext().TryGetValue(out var invocation))
        {
            invocation.SetAuditedEndpoint(id);
            if (audited)
            {
                invocation.SetAuditAction(id);
            }

            // 结果对象由端点中间件在过滤器返回之后执行，响应开始时才知道最终状态码
            http.Response.OnStarting(static state =>
            {
                var (target, response) = ((InvocationContext, HttpResponse))state;
                target.SetAuditedEndpointStatus(response.StatusCode);
                return Task.CompletedTask;
            }, (invocation, http.Response));
        }

        return await next(context).ConfigureAwait(false);
    }
}
