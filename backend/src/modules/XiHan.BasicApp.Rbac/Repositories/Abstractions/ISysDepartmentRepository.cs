#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISysDepartmentRepository
// Guid:ca2b3c4d-5e6f-7890-abcd-ef12345678a1
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 4:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Repositories.Abstractions;

/// <summary>
/// 系统部门仓储接口
/// </summary>
public interface ISysDepartmentRepository : IRepositoryBase<SysDepartment, RbacIdType>
{
    /// <summary>
    /// 根据部门编码获取部门
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <returns></returns>
    Task<SysDepartment?> GetByDepartmentCodeAsync(string departmentCode);

    /// <summary>
    /// 检查部门编码是否存在
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <param name="excludeId">排除的部门ID</param>
    /// <returns></returns>
    Task<bool> ExistsByDepartmentCodeAsync(string departmentCode, RbacIdType? excludeId = null);

    /// <summary>
    /// 获取所有根部门
    /// </summary>
    /// <returns></returns>
    Task<List<SysDepartment>> GetRootDepartmentsAsync();

    /// <summary>
    /// 根据父级ID获取子部门
    /// </summary>
    /// <param name="parentId">父级部门ID</param>
    /// <returns></returns>
    Task<List<SysDepartment>> GetChildrenAsync(RbacIdType parentId);

    /// <summary>
    /// 根据用户ID获取部门列表
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns></returns>
    Task<List<SysDepartment>> GetByUserIdAsync(RbacIdType userId);

    /// <summary>
    /// 获取部门的用户数量
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <returns></returns>
    Task<int> GetDepartmentUserCountAsync(RbacIdType departmentId);
}
