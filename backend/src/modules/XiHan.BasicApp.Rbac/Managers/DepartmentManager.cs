#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentManager
// Guid:bb2b3c4d-5e6f-7890-abcd-ef12345678b0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/31 6:10:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Repositories.Departments;
using XiHan.Framework.Domain.Services;

namespace XiHan.BasicApp.Rbac.Managers;

/// <summary>
/// 系统部门领域管理器
/// </summary>
public class DepartmentManager : DomainService
{
    private readonly ISysDepartmentRepository _departmentRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="departmentRepository">部门仓储</param>
    public DepartmentManager(ISysDepartmentRepository departmentRepository)
    {
        _departmentRepository = departmentRepository;
    }

    /// <summary>
    /// 验证部门编码是否唯一
    /// </summary>
    /// <param name="departmentCode">部门编码</param>
    /// <param name="excludeId">排除的部门ID</param>
    /// <returns></returns>
    public async Task<bool> IsDepartmentCodeUniqueAsync(string departmentCode, long? excludeId = null)
    {
        return !await _departmentRepository.ExistsByDepartmentCodeAsync(departmentCode, excludeId);
    }

    /// <summary>
    /// 检查部门是否可以删除
    /// </summary>
    /// <param name="departmentId">部门ID</param>
    /// <returns></returns>
    public async Task<bool> CanDeleteAsync(long departmentId)
    {
        var children = await _departmentRepository.GetChildrenAsync(departmentId);
        if (children.Count > 0)
        {
            return false;
        }

        var userCount = await _departmentRepository.GetDepartmentUserCountAsync(departmentId);
        return userCount == 0;
    }
}
