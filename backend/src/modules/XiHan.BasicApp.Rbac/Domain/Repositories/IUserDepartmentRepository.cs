#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserDepartmentRepository
// Guid:383558b6-9992-4394-9d10-ea47def41727
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:35:30
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Domain.Entities;
using XiHan.Framework.Domain.Repositories;

namespace XiHan.BasicApp.Rbac.Domain.Repositories;

/// <summary>
/// 用户部门关系仓储接口
/// </summary>
public interface IUserDepartmentRepository : IRepositoryBase<SysUserDepartment, long>
{
    /// <summary>
    /// 获取用户部门关系
    /// </summary>
    Task<IReadOnlyList<SysUserDepartment>> GetByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户部门关系
    /// </summary>
    Task<bool> RemoveByUserIdAsync(long userId, long? tenantId = null, CancellationToken cancellationToken = default);
}
