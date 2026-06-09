#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserDepartmentQueryService
// Guid:0d64cc2d-6dc0-43d2-9d8f-4e8e8d8af603
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 用户部门归属查询应用服务接口
/// </summary>
public interface IUserDepartmentQueryService : IApplicationService
{
    /// <summary>
    /// 获取用户部门归属列表
    /// </summary>
    /// <param name="userId">用户主键</param>
    /// <param name="onlyValid">是否仅返回有效归属</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属列表</returns>
    Task<IReadOnlyList<UserDepartmentListItemDto>> GetUserDepartmentsAsync(long userId, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门用户归属列表
    /// </summary>
    /// <param name="departmentId">部门主键</param>
    /// <param name="includeChildren">是否包含子部门</param>
    /// <param name="onlyValid">是否仅返回有效归属</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门用户归属列表</returns>
    Task<IReadOnlyList<UserDepartmentListItemDto>> GetDepartmentUsersAsync(long departmentId, bool includeChildren = false, bool onlyValid = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户部门归属详情
    /// </summary>
    /// <param name="id">用户部门归属主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户部门归属详情</returns>
    Task<UserDepartmentDetailDto?> GetUserDepartmentDetailAsync(long id, CancellationToken cancellationToken = default);
}
