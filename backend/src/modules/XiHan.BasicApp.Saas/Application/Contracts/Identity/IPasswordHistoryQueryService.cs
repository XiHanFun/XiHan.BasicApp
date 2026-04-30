#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPasswordHistoryQueryService
// Guid:af9b303c-e07a-4ab0-a06a-6f48eb516383
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
/// 密码历史查询应用服务接口
/// </summary>
public interface IPasswordHistoryQueryService : IApplicationService
{
    /// <summary>
    /// 获取密码历史分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>密码历史分页列表</returns>
    Task<PageResultDtoBase<PasswordHistoryListItemDto>> GetPasswordHistoryPageAsync(PasswordHistoryPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取密码历史详情
    /// </summary>
    /// <param name="id">密码历史主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>密码历史详情</returns>
    Task<PasswordHistoryDetailDto?> GetPasswordHistoryDetailAsync(long id, CancellationToken cancellationToken = default);
}
