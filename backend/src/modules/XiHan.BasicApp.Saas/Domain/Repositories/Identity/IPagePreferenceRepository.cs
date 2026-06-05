#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPagePreferenceRepository
// Guid:6b2d8f03-4c5e-4a9b-8d2f-1e3c7a0b5f62
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 页面偏好仓储接口
/// </summary>
public interface IPagePreferenceRepository : ISaasRepository<SysPagePreference>
{
    /// <summary>
    /// 按用户与页面码获取偏好
    /// </summary>
    Task<SysPagePreference?> GetByUserAndPageAsync(long userId, string pageCode, CancellationToken cancellationToken = default);
}
