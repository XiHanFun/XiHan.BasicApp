// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户通知偏好仓储接口
/// </summary>
public interface IUserNotificationPreferenceRepository : ISaasRepository<SysUserNotificationPreference>
{
    /// <summary>
    /// 根据用户ID获取通知偏好信息
    /// </summary>
    Task<SysUserNotificationPreference?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
