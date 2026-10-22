// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 访问策略命令应用服务接口
/// </summary>
public interface IAccessPolicyAppService : IApplicationService
{
    /// <summary>
    /// 创建访问策略（一律只记录）
    /// </summary>
    Task<AccessPolicyDetailDto> CreateAccessPolicyAsync(AccessPolicyCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新访问策略的定义与条件
    /// </summary>
    Task<AccessPolicyDetailDto> UpdateAccessPolicyAsync(AccessPolicyUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改访问策略的动作（含启用拦截）
    /// </summary>
    Task<AccessPolicyDetailDto> UpdateAccessPolicyActionAsync(AccessPolicyActionUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用或停用访问策略
    /// </summary>
    Task<AccessPolicyDetailDto> UpdateAccessPolicyStatusAsync(AccessPolicyStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除访问策略
    /// </summary>
    Task DeleteAccessPolicyAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 手动解除自动封禁
    /// </summary>
    Task<bool> UnbanLoginAsync(LoginUnbanDto input, CancellationToken cancellationToken = default);
}
