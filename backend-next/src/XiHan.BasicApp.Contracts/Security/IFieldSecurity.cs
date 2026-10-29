// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 解析当前调用主体在一个实体上的字段规则。
/// </summary>
/// <remarks>
/// 方案 5.9 字段安全。由 Access 包实现：规则按目标（角色、用户、部门）命中当前主体，同一侧（平台或本租户）的规则才算数；
/// 各包经 <c>Runtime.FieldSecurityQuery</c> 消费，没有实现时一律没有规则。实体须在 <see cref="FieldSecurityCatalogSection"/> 里登记过。
/// </remarks>
public interface IFieldSecurity
{
    /// <summary>
    /// 解析当前主体在实体上的有效规则。
    /// </summary>
    /// <remarks>
    /// 同一次调用内可缓存。
    /// </remarks>
    ValueTask<FieldRules> ResolveAsync(string entity, InvocationContext context);
}
