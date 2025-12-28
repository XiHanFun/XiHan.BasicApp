#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDepartmentService
// Guid:2b2b3c4d-5e6f-7890-abcd-ef12345678a7
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 5:25:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Services.Departments.Dtos;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Rbac.Services.Departments;

/// <summary>
/// 系统部门服务接口
/// </summary>
public interface ISysDepartmentService : ICrudApplicationService<DepartmentDto, XiHanBasicAppIdType, CreateDepartmentDto, UpdateDepartmentDto>
{
    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <returns></returns>
    Task<DepartmentDto?> GetByDepartmentCodeAsync(string departmentCode);

    /// <summary>
    /// 获取部门树
    /// </summary>
    /// <returns></returns>
    Task<List<DepartmentTreeDto>> GetTreeAsync();

    /// <summary>
    /// 根据父级ID获取子部门
    /// </summary>
    /// <param name="parentId">父级部门ID</param>
    /// <returns></returns>
    Task<List<DepartmentDto>> GetChildrenAsync(XiHanBasicAppIdType parentId);

    /// <summary>
    /// 获取用户的部门列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<DepartmentDto>> GetByUserIdAsync(XiHanBasicAppIdType userId);
}
