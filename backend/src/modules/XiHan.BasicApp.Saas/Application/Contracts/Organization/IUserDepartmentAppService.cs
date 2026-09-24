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
    /// 批量变更用户部门归属（一次性提交分配与撤销）
    /// </summary>
    /// <param name="input">批量变更参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task BatchUpdateUserDepartmentsAsync(UserDepartmentBatchUpdateDto input, CancellationToken cancellationToken = default);

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
}
