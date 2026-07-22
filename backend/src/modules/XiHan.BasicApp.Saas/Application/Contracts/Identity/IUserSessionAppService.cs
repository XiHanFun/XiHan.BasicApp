// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户会话命令应用服务接口
/// </summary>
public interface IUserSessionAppService : IApplicationService
{
    /// <summary>
    /// 撤销用户会话
    /// </summary>
    /// <param name="input">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户会话详情</returns>
    Task<UserSessionDetailDto> RevokeUserSessionAsync(UserSessionRevokeDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户全部会话
    /// </summary>
    /// <param name="input">撤销参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>撤销会话数量</returns>
    Task<int> RevokeUserSessionsAsync(UserSessionsRevokeDto input, CancellationToken cancellationToken = default);
}
