// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户部门命令应用服务接口
/// </summary>
public interface IUserDepartmentAppService : IApplicationService
{
    /// <summary>
    /// 分配用户部门归属
    /// </summary>
    /// <param name="input">分配参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    Task<UserDepartmentDetailDto> CreateUserDepartmentAsync(UserDepartmentAssignDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户部门归属
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    Task<UserDepartmentDetailDto> UpdateUserDepartmentAsync(UserDepartmentUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户部门归属状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    Task<UserDepartmentDetailDto> UpdateUserDepartmentStatusAsync(UserDepartmentStatusUpdateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 撤销用户部门归属
    /// </summary>
    /// <param name="id">用户部门归属主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteUserDepartmentAsync(long id, CancellationToken cancellationToken = default);
}
