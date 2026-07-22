// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 在线用户查询应用服务接口
/// </summary>
/// <remarks>
/// 在线 = 活跃会话（数据库 SysUserSession.Status==Active 且未过期）+ 实时连接标注（SignalR 连接管理器）。
/// 强制下线复用用户会话命令服务（saas:user-session:revoke）。
/// </remarks>
public interface IOnlineUserQueryService : IApplicationService
{
    /// <summary>
    /// 获取在线用户分页列表（一行 = 一个活跃会话）
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>在线用户分页列表</returns>
    Task<PageResultDtoBase<OnlineUserListItemDto>> GetOnlineUserPageAsync(OnlineUserPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取在线用户概览（实时在线数/活跃会话数/活跃用户数）
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>在线用户概览</returns>
    Task<OnlineUserSummaryDto> GetOnlineUserSummaryAsync(CancellationToken cancellationToken = default);
}
