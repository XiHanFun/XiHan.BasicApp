#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPositionQueryService
// Guid:c354219e-172e-42a3-360b-142536475860
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 岗位查询应用服务接口
/// </summary>
public interface IPositionQueryService : IApplicationService
{
    /// <summary>
    /// 获取岗位分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>岗位分页列表</returns>
    Task<PageResultDtoBase<PositionListItemDto>> GetPositionPageAsync(PositionPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取岗位详情
    /// </summary>
    /// <param name="id">岗位主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>岗位详情</returns>
    Task<PositionDetailDto?> GetPositionDetailAsync(long id, CancellationToken cancellationToken = default);
}
