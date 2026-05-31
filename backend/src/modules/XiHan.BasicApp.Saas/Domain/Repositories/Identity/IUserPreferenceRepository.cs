#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserPreferenceRepository
// Guid:c7d2a3e4-9b8f-4a1c-b6e5-3f0a8d2c1e47
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户偏好仓储接口
/// </summary>
public interface IUserPreferenceRepository : ISaasRepository<SysUserPreference>
{
    /// <summary>
    /// 根据用户ID获取偏好信息
    /// </summary>
    Task<SysUserPreference?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
