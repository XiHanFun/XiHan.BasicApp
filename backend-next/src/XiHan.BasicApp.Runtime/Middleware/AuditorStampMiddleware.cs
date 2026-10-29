// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Middleware;

/// <summary>
/// 把当前已认证主体的 ID 交给操作人戳的中间件。
/// </summary>
/// <remarks>
/// Transaction 阶段中间件：把当前已认证主体的 ID 放进 <see cref="AuditorStamp"/>，让同一调用里 SqlSugar 的 <c>AopEvents.DataExecuting</c> 回调能填 <c>CreatedBy</c> / <c>ModifiedBy</c> / <c>DeletedBy</c>。
/// 方式一的 <c>ConnectionConfig</c> 在装配时只建一份、没有作用域，回调拿不到作用域服务，所以主体经异步本地值传递（N0 冒烟结论，见方案 9.1）。
/// </remarks>
public sealed class AuditorStampMiddleware : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        var principal = context.GetSecurity().Principal;
        using (AuditorStamp.Enter(principal.IsAuthenticated ? principal.Id : null))
        {
            await nextStep(context).ConfigureAwait(false);
        }
    }
}
