// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 字段安全的响应过滤器。
/// </summary>
/// <remarks>
/// 框架在操作成功、编码之前调用（ADR-0061），HTTP、gRPC、MCP 与内部调用拿到的都是套过掩码的响应；绑定与掩码在 <see cref="FieldSecurityBindings"/>。
/// </remarks>
/// <param name="bindings">各包登记的操作绑定。</param>
public sealed class FieldSecurityResponseFilter(FieldSecurityBindings bindings) : IOperationResponseFilter
{
    /// <inheritdoc />
    public ValueTask<object> FilterAsync(InvocationContext context, OperationDescriptor operation, object response)
        => bindings.MaskAsync(context, operation, response);
}
