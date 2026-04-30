#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserDepartmentAppService
// Guid:4f3574d1-0cd6-4303-bb08-21877e1f857e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户部门归属命令应用服务接口
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
