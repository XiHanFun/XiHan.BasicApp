// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;

namespace XiHan.BasicApp.Saas.Infrastructure.Repositories;

/// <summary>
/// 用户设置仓储实现
/// </summary>
public sealed class UserSettingRepository(ISqlSugarClientResolver clientResolver)
    : SaasRepository<SysUserSetting>(clientResolver), IUserSettingRepository
{
    /// <summary>
    /// 按用户 × 场景 × 设置键获取设置（跨租户）
    /// </summary>
    /// <remarks>
    /// 唯一索引 UX_UsId_Sc_SeKe 不含租户：每个键全局仅一行，行带的是首次保存时所在租户的戳。
    /// 带租户过滤会在别的租户里查不到而再插一行，撞唯一索引。
    /// </remarks>
    public async Task<SysUserSetting?> GetByUserSettingAsync(long userId, UserSettingScene scene, string settingKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await CreateNoTenantQueryable()
            .Where(setting => setting.UserId == userId && setting.Scene == scene && setting.SettingKey == settingKey)
            .FirstAsync(cancellationToken);
    }
}
