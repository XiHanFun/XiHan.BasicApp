#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentQueryService
// Guid:31d65ab1-c2a1-44ed-9eb5-011ca0669c39
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 部门查询应用服务接口
/// </summary>
public interface IDepartmentQueryService : IApplicationService
{
    /// <summary>
    /// 获取部门分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门分页列表</returns>
    Task<PageResultDtoBase<DepartmentListItemDto>> GetDepartmentPageAsync(DepartmentPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门详情
    /// </summary>
    /// <param name="id">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    Task<DepartmentDetailDto?> GetDepartmentDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门树
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门树</returns>
    Task<IReadOnlyList<DepartmentTreeNodeDto>> GetDepartmentTreeAsync(DepartmentTreeQueryDto input, CancellationToken cancellationToken = default);
}
