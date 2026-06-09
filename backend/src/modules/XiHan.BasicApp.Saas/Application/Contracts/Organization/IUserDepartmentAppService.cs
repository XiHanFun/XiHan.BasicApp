#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserDepartmentAppService
// Guid:6c7d8e9f-0a1b-4c2d-3e4f-5a6b7c8d9e0f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
