#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISessionRoleQueryService
// Guid:a6a70602-b288-45e7-af6e-210130b6d597
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 会话角色查询应用服务接口
/// </summary>
public interface ISessionRoleQueryService : IApplicationService
{
    /// <summary>
    /// 获取会话角色分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话角色分页列表</returns>
    Task<PageResultDtoBase<SessionRoleListItemDto>> GetSessionRolePageAsync(SessionRolePageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取会话角色详情
    /// </summary>
    /// <param name="id">会话角色主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>会话角色详情</returns>
    Task<SessionRoleDetailDto?> GetSessionRoleDetailAsync(long id, CancellationToken cancellationToken = default);
}
