#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IOperationQueryService
// Guid:e178bd76-7672-4323-b47e-dc0c3d8a0c9d
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
/// 操作查询应用服务接口
/// </summary>
public interface IOperationQueryService : IApplicationService
{
    /// <summary>
    /// 获取操作分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作分页列表</returns>
    Task<PageResultDtoBase<OperationListItemDto>> GetOperationPageAsync(OperationPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取操作详情
    /// </summary>
    /// <param name="id">操作主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作详情</returns>
    Task<OperationDetailDto?> GetOperationDetailAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取可选全局操作列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>可选全局操作列表</returns>
    Task<IReadOnlyList<OperationSelectItemDto>> GetAvailableGlobalOperationsAsync(OperationSelectQueryDto input, CancellationToken cancellationToken = default);
}
