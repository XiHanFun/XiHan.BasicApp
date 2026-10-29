// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 解析当前调用主体的数据范围。
/// </summary>
/// <remarks>
/// 由 Access 包实现（按角色的 <c>DataScope</c> 与自定义部门求并集，再经 Organization 的归属与闭包表展开成用户集合）；
/// Identity、Organization 等列表经 <c>Runtime.DataScopeQuery</c> 消费。没有实现时视为不受限（没有装 Access 就没有数据范围这个概念）。
/// </remarks>
public interface IDataScopeResolver
{
    /// <summary>
    /// 解析当前主体的数据范围判定。
    /// </summary>
    /// <remarks>
    /// 同一次调用内可缓存。
    /// </remarks>
    ValueTask<DataScopeDecision> ResolveAsync(InvocationContext context);
}
