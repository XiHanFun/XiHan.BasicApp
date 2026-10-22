// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Authorization.Conditions;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 条件属性来源工厂
/// </summary>
public interface ISaasConditionAttributeSourceFactory
{
    /// <summary>
    /// 当前请求的取值上下文：当前用户、当前租户、当前请求
    /// </summary>
    /// <param name="now">求值时刻</param>
    /// <returns>取值上下文</returns>
    SaasConditionContext CreateCurrentContext(DateTimeOffset now);

    /// <summary>
    /// 按上下文建一个属性来源：惰性取值，同一来源里每个属性只取一次
    /// </summary>
    /// <param name="context">取值上下文</param>
    /// <returns>属性来源</returns>
    IConditionAttributeSource Create(SaasConditionContext context);
}
