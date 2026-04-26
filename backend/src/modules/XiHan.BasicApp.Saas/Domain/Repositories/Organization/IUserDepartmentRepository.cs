#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserDepartmentRepository
// Guid:f890e8bd-a848-486f-b6fa-df4a7f708872
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户部门仓储接口
/// </summary>
public interface IUserDepartmentRepository : ISaasRepository<SysUserDepartment>
{
    /// <summary>
    /// 获取用户有效部门归属
    /// </summary>
    Task<IReadOnlyList<SysUserDepartment>> GetValidByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
