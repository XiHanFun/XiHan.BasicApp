#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IDepartmentDomainService
// Guid:3b4c5d6e-7f80-9012-bcde-f01234567803
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 部门领域服务接口
/// </summary>
public interface IDepartmentDomainService
{
    /// <summary>
    /// 创建部门
    /// </summary>
    Task<SysDepartment> CreateAsync(SysDepartment department);

    /// <summary>
    /// 更新部门
    /// </summary>
    Task<SysDepartment> UpdateAsync(SysDepartment department);

    /// <summary>
    /// 删除部门
    /// </summary>
    Task<bool> DeleteAsync(long id);
}
