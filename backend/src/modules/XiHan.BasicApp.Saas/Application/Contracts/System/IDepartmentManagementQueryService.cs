// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 部门管理页面查询应用服务接口
/// </summary>
public interface IDepartmentManagementQueryService : IApplicationService
{
    /// <summary>
    /// 获取部门管理详情聚合数据
    /// </summary>
    Task<DepartmentManagementDetailDto?> GetDepartmentManagementDetailAsync(long departmentId, CancellationToken cancellationToken = default);
}
