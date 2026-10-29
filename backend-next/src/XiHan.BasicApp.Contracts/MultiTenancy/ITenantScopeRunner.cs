// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.MultiTenancy;

/// <summary>
/// 以指定租户发起一次独立调用。
/// </summary>
/// <remarks>
/// 走完整内核管道：租户解析与校验、工作单元按该租户打开。
/// 平台侧的种子、到期处理、日志清理等要写租户库实体时用它，不在同一上下文里切换租户（方案 5.5）。
/// </remarks>
public interface ITenantScopeRunner
{
    /// <summary>
    /// 为 <paramref name="tenantId"/> 开一次独立调用并执行 <paramref name="action"/>。
    /// </summary>
    /// <returns>调用的结果；调用失败（租户不存在、已停用、动作返回失败）即回滚并返回该失败。</returns>
    Task<Result> RunAsync(string tenantId, Func<InvocationContext, ValueTask<Result>> action, CancellationToken cancellationToken);
}
