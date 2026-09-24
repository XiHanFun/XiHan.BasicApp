// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户数据范围命令应用服务接口
/// </summary>
public interface IUserDataScopeAppService : IApplicationService
{
    /// <summary>
    /// 批量变更用户数据范围（一次性提交授予与撤销）
    /// </summary>
    /// <param name="input">批量变更参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BatchUpdateUserDataScopesAsync(UserDataScopeBatchUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户数据范围
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    Task<UserDataScopeDetailDto> UpdateUserDataScopeAsync(UserDataScopeUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户数据范围状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户数据范围详情</returns>
    Task<UserDataScopeDetailDto> UpdateUserDataScopeStatusAsync(UserDataScopeStatusUpdateDto input, CancellationToken cancellationToken = default);
}
