// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 用户设置仓储接口
/// </summary>
public interface IUserSettingRepository : ISaasRepository<SysUserSetting>
{
    /// <summary>
    /// 按用户 × 场景 × 设置键获取设置
    /// </summary>
    Task<SysUserSetting?> GetByUserSettingAsync(long userId, UserSettingScene scene, string settingKey, CancellationToken cancellationToken = default);
}
