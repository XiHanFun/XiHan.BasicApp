// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.Security;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Runtime.Queries;

/// <summary>
/// 字段安全：取当前主体在一个实体上的字段规则。
/// </summary>
/// <remarks>
/// 方案 5.9。没有装 Access 的宿主一律没有规则。
/// </remarks>
public static class FieldSecurityQuery
{
    /// <summary>
    /// 取当前主体在实体上的有效字段规则。
    /// </summary>
    /// <remarks>
    /// 有 <see cref="IFieldSecurity"/> 就问它，没有即 <see cref="FieldRules.None"/>。
    /// </remarks>
    public static ValueTask<FieldRules> ResolveAsync(InvocationContext context, string entity)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(entity);
        var security = context.Services.GetService<IFieldSecurity>();
        return security is null ? ValueTask.FromResult(FieldRules.None) : security.ResolveAsync(entity, context);
    }
}
