// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Users;

/// <summary>
/// 接收人来源（通知按目标找人）。
/// </summary>
/// <remarks>
/// <see cref="RecipientKind.All"/> 与 <see cref="RecipientKind.User"/> 由 Identity 实现，
/// <see cref="RecipientKind.Role"/> 由 Access、<see cref="RecipientKind.Department"/> 由 Organization 实现。
/// <see cref="RecipientKind.User"/> 同时是最后一道筛：只留下当前一侧此刻正常的账号（别的来源给出的候选都要再经它）。
/// </remarks>
public interface IRecipientSource
{
    /// <summary>
    /// 本来源解析的目标种类。
    /// </summary>
    RecipientKind Kind { get; }

    /// <summary>
    /// 在调用的工作单元里按目标解析出用户 ID。
    /// </summary>
    /// <remarks>
    /// 侧取调用的租户（没有即平台）。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    /// <param name="targetIds">目标 ID（角色、部门或用户）；<see cref="RecipientKind.All"/> 忽略。</param>
    /// <returns>用户 ID（去重）。</returns>
    Task<IReadOnlyCollection<Guid>> ResolveAsync(InvocationContext context, IReadOnlyCollection<Guid> targetIds);
}
