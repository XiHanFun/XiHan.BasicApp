// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 访问策略领域服务：只维护当前层（平台或当前租户）的策略
/// </summary>
public interface IAccessPolicyDomainService
{
    /// <summary>
    /// 创建访问策略，动作一律是只记录
    /// </summary>
    Task<AccessPolicyCommandResult> CreateAccessPolicyAsync(AccessPolicyCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新访问策略的定义与条件
    /// </summary>
    Task<AccessPolicyCommandResult> UpdateAccessPolicyAsync(AccessPolicyUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改访问策略的动作
    /// </summary>
    Task<AccessPolicyCommandResult> UpdateAccessPolicyActionAsync(AccessPolicyActionCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用或停用访问策略
    /// </summary>
    Task<AccessPolicyCommandResult> UpdateAccessPolicyStatusAsync(AccessPolicyStatusCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除访问策略及其条件
    /// </summary>
    Task DeleteAccessPolicyAsync(long id, CancellationToken cancellationToken = default);
}
