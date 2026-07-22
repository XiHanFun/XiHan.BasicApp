// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
