#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserDataScopeRepository
// Guid:70fa511c-5666-4962-830c-4da0e7b54cef
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户数据范围仓储接口
/// </summary>
public interface IUserDataScopeRepository : ISaasRepository<SysUserDataScope>
{
    /// <summary>
    /// 获取用户有效数据范围覆盖
    /// </summary>
    Task<IReadOnlyList<SysUserDataScope>> GetValidByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
