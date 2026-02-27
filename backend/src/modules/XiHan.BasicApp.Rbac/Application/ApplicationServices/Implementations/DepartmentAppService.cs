#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentAppService
// Guid:70baa84a-073e-4f73-84bb-643c6d522be0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 06:29:49
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Mapster;
using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Rbac.Domain.Repositories;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices.Implementations;

/// <summary>
/// 部门应用服务
/// </summary>
public class DepartmentAppService : ApplicationServiceBase, IDepartmentAppService
{
    private readonly IDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="departmentRepository"></param>
    public DepartmentAppService(IDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 根据部门ID获取部门
    /// </summary>
    /// <param name="departmentId"></param>
    /// <returns></returns>
    public async Task<DepartmentDto?> GetByIdAsync(long departmentId)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId);
        return department?.Adapt<DepartmentDto>();
    }

    /// <summary>
    /// 获取子部门
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    public async Task<IReadOnlyList<DepartmentDto>> GetChildrenAsync(long? parentId, long? tenantId = null)
    {
        var departments = await _departmentRepository.GetChildrenAsync(parentId, tenantId);
        return departments.Select(static department => department.Adapt<DepartmentDto>()).ToArray();
    }
}
