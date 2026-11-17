// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Schema;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Kernel.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Middleware;

/// <summary>
/// 独立库没有升级到当前结构版本的租户，调用一律暂不可用
/// </summary>
/// <remarks>
/// Tenant 阶段、租户解析之后：调用的租户在 <see cref="SchemaHealth"/> 里即 <see cref="RuntimeErrors.TenantSchemaUnavailable"/>（503），不让业务在旧结构上读写；
/// 平台一侧的调用不受影响，管理员照常在维护页里预览与重试这个租户库的升级
/// </remarks>
/// <param name="health">不可用的租户独立库</param>
public sealed class TenantSchemaGateMiddleware(SchemaHealth health) : IPipelineMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(InvocationContext context, PipelineStep nextStep)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(nextStep);
        if (context.GetSecurity().Tenant?.Id is { } tenant && health.IsUnavailable(tenant))
        {
            context.SetOutcome(Error.Unavailable(RuntimeErrors.TenantSchemaUnavailable, RuntimeMessages.TenantSchemaUnavailable, MessageArguments.Of(("tenant", tenant))));
            return;
        }

        await nextStep(context).ConfigureAwait(false);
    }
}
