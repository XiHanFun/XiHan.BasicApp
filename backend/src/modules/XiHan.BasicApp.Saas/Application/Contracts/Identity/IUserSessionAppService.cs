#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSessionAppService
// Guid:614eb399-1ca1-4787-b1d6-157c2926e6d8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
