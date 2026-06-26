#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentManagementQueryService
// Guid:58d25cd4-1b69-4159-97e9-a662cf113475
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/20 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
