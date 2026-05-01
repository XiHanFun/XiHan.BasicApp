#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDictQueryService
// Guid:c83e3d2e-dab9-4b64-80fa-803670fe3d18
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
/// 系统字典查询应用服务接口
/// </summary>
public interface IDictQueryService : IApplicationService
{
    /// <summary>
    /// 获取系统字典分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典分页列表</returns>
    Task<PageResultDtoBase<DictListItemDto>> GetDictPageAsync(DictPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统字典详情
    /// </summary>
    /// <param name="id">系统字典主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典详情</returns>
    Task<DictDetailDto?> GetDictDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统字典项分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典项分页列表</returns>
    Task<PageResultDtoBase<DictItemListItemDto>> GetDictItemPageAsync(DictItemPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统字典项详情
    /// </summary>
    /// <param name="id">系统字典项主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典项详情</returns>
    Task<DictItemDetailDto?> GetDictItemDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取系统字典项树
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统字典项树</returns>
    Task<IReadOnlyList<DictItemTreeNodeDto>> GetDictItemTreeAsync(DictItemTreeQueryDto input, CancellationToken cancellationToken = default);
}
