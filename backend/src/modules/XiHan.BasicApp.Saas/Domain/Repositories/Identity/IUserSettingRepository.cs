#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserSettingRepository
// Guid:6b2d8f03-4c5e-4a9b-8d2f-1e3c7a0b5f62
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/10 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
