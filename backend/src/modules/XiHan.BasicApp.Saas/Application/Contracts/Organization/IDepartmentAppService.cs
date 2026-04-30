#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentAppService
// Guid:7fa63f11-5538-4f65-a5df-9783cc86d1f1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 部门命令应用服务接口
/// </summary>
public interface IDepartmentAppService : IApplicationService
{
    /// <summary>
    /// 创建部门
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    Task<DepartmentDetailDto> CreateDepartmentAsync(DepartmentCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    Task<DepartmentDetailDto> UpdateDepartmentAsync(DepartmentUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    Task<DepartmentDetailDto> UpdateDepartmentStatusAsync(DepartmentStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除部门
    /// </summary>
    /// <param name="id">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken = default);
}
